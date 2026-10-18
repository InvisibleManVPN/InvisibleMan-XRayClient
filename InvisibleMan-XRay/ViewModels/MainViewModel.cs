using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InvisibleManXRay.ViewModels
{
    using Models;
    using Values;
    using Services;
    using Services.UI;
    using Services.Analytics.MainWindow;

    /// <summary>
    /// MVVM-модель главного окна (Шаг 4, CommunityToolkit.Mvvm).
    /// Инкапсулирует конвейер подключения через <see cref="IConnectionService"/>
    /// и прокидывает события аналитики. UI-поток не блокируется: все
    /// блокирующие вызовы ядра выполняются внутри ConnectionService (BackgroundWorker → Task).
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        /// <summary>Три состояния Hero-кнопки (Пункт 2 ТЗ).</summary>
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected
        }

        private readonly IConnectionService connectionService;

        // Токен активной фазы «Подключение» — отменяет ожидание ConnectAsync по Cancel.
        private CancellationTokenSource cancellationTokenSource;

        // Колбэки legacy-обвязки MainWindow (окна, политика, приватность).
        private Func<Config> getConfig = () => null;
        private Action openServerWindowAction = () => { };
        private Action openSettingsWindowAction = () => { };
        private Action openUpdateWindowAction = () => { };
        private Action openAboutWindowAction = () => { };
        private Action onGitHubClickAction = () => { };
        private Action onBugReportingClickAction = () => { };

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        /// <summary>Текущее состояние подключения — управляет Hero-зоной.</summary>
        [ObservableProperty]
        private ConnectionState state = ConnectionState.Disconnected;

        /// <summary>Имя активного конфига под Hero-кнопкой.</summary>
        [ObservableProperty]
        private string activeConfigName = string.Empty;

        /// <summary>Доступно ли нажатие основной кнопки (нет двух кликов подряд).</summary>
        [ObservableProperty]
        private bool isActionEnabled = true;

        /// <summary>Видимость баннера «доступно обновление».</summary>
        [ObservableProperty]
        private bool isUpdateAvailable;

        public MainViewModel(IConnectionService connectionService)
        {
            this.connectionService = connectionService;

            // Мгновенный переход Connecting → Connected при входе worker'а
            // в удержание соединения (не дожидаясь завершения ConnectAsync).
            if (connectionService is ConnectionService concrete)
                concrete.ProgressChanged += OnConnectionProgress;
        }

        private void OnConnectionProgress(object sender, Status status)
        {
            if (status.Code != Code.SUCCESS || status.SubCode != SubCode.RUNNING)
                return;

            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (State == ConnectionState.Connecting)
                    State = ConnectionState.Connected;
            }));
        }

        /// <summary>
        /// Поздняя инициализация (после Setup() MainWindow): делегаты окон + локализация.
        /// </summary>
        public void Initialize(
            Func<Config> getConfig,
            Action openServerWindow,
            Action openSettingsWindow,
            Action openUpdateWindow,
            Action openAboutWindow,
            Action onGitHubClick,
            Action onBugReportingClick)
        {
            this.getConfig = getConfig;
            this.openServerWindowAction = openServerWindow;
            this.openSettingsWindowAction = openSettingsWindow;
            this.openUpdateWindowAction = openUpdateWindow;
            this.openAboutWindowAction = openAboutWindow;
            this.onGitHubClickAction = onGitHubClick;
            this.onBugReportingClickAction = onBugReportingClick;

            RefreshConfigName();
        }

        partial void OnStateChanged(ConnectionState value)
        {
            // Single source of truth для legacy-визуалов MainWindow (code-behind подписан на это событие).
            StateChangedExternally?.Invoke(this, value);
        }

        /// <summary>Событие смены состояния — мост ViewModel → визуалы MainWindow.</summary>
        public event EventHandler<ConnectionState> StateChangedExternally;

        public void SetUpdateAvailable(bool available) => IsUpdateAvailable = available;

        public void RefreshConfigName()
        {
            Config config = getConfig.Invoke();

            ActiveConfigName = config != null
                ? config.Name
                : LocalizationService.GetTerm(Localization.NO_SERVER_CONFIGURATION);
        }

        // ============================================================
        //  КОМАНДЫ (CommunityToolkit.Mvvm)
        // ============================================================

        /// <summary>
        /// Hero-команда: Disconnected → Connect, Connected → Stop,
        /// Connecting → Cancel. Единая точка входа кнопки и хоткея (Шаг 11).
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsActionEnabled))]
        private async Task ToggleConnectionAsync()
        {
            switch (State)
            {
                case ConnectionState.Disconnected:
                    await ConnectAsync();
                    break;

                case ConnectionState.Connecting:
                    CancelConnecting();
                    break;

                case ConnectionState.Connected:
                    Stop();
                    break;
            }
        }

        /// <summary>Подключение без блокировки UI-потока.</summary>
        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (State != ConnectionState.Disconnected)
                return;

            IsActionEnabled = false;
            State = ConnectionState.Connecting;
            AnalyticsService.SendEvent(new RunButtonClickedEvent());

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            // ВАЖНО: await без ConfigureAwait(true) — continuation резюмируется на UI-потоке
            // автоматически (SynchronizationContext), при этом сам awaited Task не захватывает
            // контекст → нет deadlock при вызове ConnectAsyncCommand.Execute из non-UI кода
            // (трей/хоткей на Шагах 7/10/11).
            Status status = await connectionService
                .ConnectAsync(cancellationTokenSource.Token);

            // Цикл мог быть отменён через CancellationToken (CancelConnecting) —
            // итоговое состояние уже выставлено там, не перетираем.
            if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                return;

            // Соединение установлено и удерживается (RUNNING) → Connected.
            // ERROR / INFO+CANCELED / SUCCESS (стоп или rerun-остановка) → Disconnected.
            // Rerun: worker перезапускается внутри ConnectionService — вторая волна
            // ConnectAsync завершится уже с итоговым статусом нового цикла.
            if (status.Code == Code.SUCCESS && status.SubCode == SubCode.RUNNING)
                State = ConnectionState.Connected;
            else
                State = ConnectionState.Disconnected;

            // Ошибки pipeline (NO_CONFIG / INVALID_CONFIG / CANT_PROXY / CANT_TUNNEL).
            if (status.Code == Code.ERROR)
                HandleConnectionError(status);

                IsActionEnabled = true;
            }
        }

        /// <summary>Отмена фазы «Подключение».</summary>
        [RelayCommand]
        private void CancelConnecting()
        {
            if (State != ConnectionState.Connecting)
                return;

            // Отключаем expectation-обработчик ConnectAsync (он выйдет по токену)
            // и выставляем Disconnected сразу — как legacy runWorker cancel flow.
            cancellationTokenSource?.Cancel();
            connectionService.Cancel();

            State = ConnectionState.Disconnected;
            IsActionEnabled = true;
        }

        /// <summary>Остановка активного соединения + выключение режима.</summary>
        [RelayCommand]
        private void Stop()
        {
            if (State != ConnectionState.Connected)
                return;

            AnalyticsService.SendEvent(new StopButtonClickedEvent());
            connectionService.StopWithDisabledMode();
            State = ConnectionState.Disconnected;
        }

        /// <summary>
        /// Rerun при смене конфигурации (вызывается из WindowFactory.UpdateConfig).
        /// Semantics как legacy runWorker: если worker не запущен — просто обновить имя конфига.
        /// </summary>
        [RelayCommand]
        private void Restart()
        {
            RefreshConfigName();

            if (!connectionService.IsBusy)
                return;

            connectionService.Restart();
        }

        /// <summary>
        /// Rerun со сменой режима (System Proxy ↔ TUN) из настроек.
        /// Если worker запущен — перезапуск; иначе — только выключение режима (как legacy).
        /// </summary>
        [RelayCommand]
        private void RestartWithDisabledMode()
        {
            if (connectionService.IsBusy)
            {
                connectionService.RestartWithDisabledMode();
                return;
            }

            connectionService.DisableModeOnly();
        }

        /// <summary>Автоподключение при старте (настройка AutoConnect).</summary>
        [RelayCommand]
        private Task AutoConnectAsync() => ConnectAsync();

        // ============================================================
        //  НАВИГАЦИЯ / ФУТЕР (команды вместо Click-обработчиков)
        // ============================================================

        [RelayCommand]
        private void OpenServers()
        {
            openServerWindowAction.Invoke();
            AnalyticsService.SendEvent(new ManageServersButtonClickedEvent());
        }

        [RelayCommand]
        private void OpenSettings()
        {
            openSettingsWindowAction.Invoke();
            AnalyticsService.SendEvent(new SettingsButtonClickedEvent());
        }

        [RelayCommand]
        private void OpenUpdate()
        {
            openUpdateWindowAction.Invoke();
            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        [RelayCommand]
        private void OpenAbout()
        {
            openAboutWindowAction.Invoke();
            AnalyticsService.SendEvent(new AboutButtonClickedEvent());
        }

        [RelayCommand]
        private void OpenGitHub()
        {
            onGitHubClickAction.Invoke();
            AnalyticsService.SendEvent(new GitHubButtonClickedEvent());
        }

        [RelayCommand]
        private void OpenBugReporting()
        {
            onBugReportingClickAction.Invoke();
            AnalyticsService.SendEvent(new BugReportingButtonClickedEvent());
        }

        // ============================================================
        //  ОШИБКИ
        // ============================================================

        /// <summary>
        /// Единая обработка ошибок подключения (1:1 с legacy runWorker.HandleError):
        /// NO_CONFIG → warning + окно серверов; INVALID_CONFIG / CANT_PROXY / CANT_TUNNEL → error.
        /// Вызывается на UI-потоке (после await ConnectAsync).
        /// </summary>
        public void HandleConnectionError(Status status)
        {
            switch (status.SubCode)
            {
                case SubCode.NO_CONFIG:
                    MessageBoxResult result = MessageBox.Show(
                        Application.Current?.MainWindow,
                        status.Content.ToString(),
                        Caption.WARNING,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.OK)
                        OpenServers();
                    break;

                case SubCode.INVALID_CONFIG:
                case SubCode.CANT_PROXY:
                case SubCode.CANT_TUNNEL:
                    MessageBox.Show(
                        Application.Current?.MainWindow,
                        status.Content.ToString(),
                        Caption.ERROR,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    break;
            }
        }
    }
}
