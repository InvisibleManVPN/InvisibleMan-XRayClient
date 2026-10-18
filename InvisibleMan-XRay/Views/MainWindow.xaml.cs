using System;
using System.Windows;
using System.ComponentModel;

namespace InvisibleManXRay
{
    using Wpf.Ui.Controls;
    using Models;
    using Values;
    using Services;
    using Services.UI;
    using ViewModels;
    using Services.Analytics.General;
    using Services.Analytics.MainWindow;

    public partial class MainWindow : UiWindow
    {
        /// <summary>MVVM-модель (Шаг 4). Hero-кнопка, статус, команды окон.</summary>
        public MainViewModel ViewModel { get; private set; }

        private Func<bool> isNeedToShowPolicyWindow;
        private Func<bool> shouldStartHidden;
        private Func<bool> isNeedToAutoConnect;
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<Status> checkForBroadcast;
        private Func<ServerWindow> openServerWindow;
        private Func<SettingsWindow> openSettingsWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Func<PolicyWindow> openPolicyWindow;
        private Action<string> onRunServer;
        private Action onCancelServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGenerateClientId;
        private Action onGitHubClick;
        private Action onBugReportingClick;
        private Action<string> onCustomLinkClick;

        private BackgroundWorker updateWorker;
        private BackgroundWorker broadcastWorker;

        /// <summary>Один раз за жизненный цикл окна — guard для ConnectionService.Setup.</summary>
        private bool isConnectionPipelineInitialized;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
            InitializeNavigation();
            InitializeUpdateWorker();
            InitializeBroadcastWorker();

            updateWorker.RunWorkerAsync();
            broadcastWorker.RunWorkerAsync();

            void InitializeNavigation()
            {
                navMenu.SelectionChanged += (sender, e) =>
                {
                    if (e.Source != navMenu)
                        return;

                    string page = (navMenu.SelectedItem as ListBoxItem)?.Tag as string ?? "profile";

                    pageProfile.Visibility = page == "profile" ? Visibility.Visible : Visibility.Collapsed;
                    pageServers.Visibility = page == "servers" ? Visibility.Visible : Visibility.Collapsed;
                    pageSettings.Visibility = page == "settings" ? Visibility.Visible : Visibility.Collapsed;
                };
            }

            // NOTE (Шаг 4): runWorker удалён — конвейер подключения
            // (loadConfig → enableMode → runServer) инкапсулирован в ConnectionService,
            // а состояние Hero-зоны управляется только MainViewModel.State.

            void InitializeUpdateWorker()
            {
                updateWorker = new BackgroundWorker();

                updateWorker.DoWork += (sender, e) => {
                    Status updateStatus = checkForUpdate.Invoke();
                    if (IsUpdateAvailable())
                        Dispatcher.BeginInvoke(new Action(delegate {
                            notificationUpdate.Visibility = Visibility.Visible;
                        }));

                    bool IsUpdateAvailable() => updateStatus.SubCode == SubCode.UPDATE_AVAILABLE;
                };
            }

            void InitializeBroadcastWorker()
            {
                broadcastWorker = new BackgroundWorker();

                broadcastWorker.DoWork += (sender, e) => {
                    Status broadcastStatus = checkForBroadcast.Invoke();
                    if (IsBroadcastAvailable())
                        Dispatcher.BeginInvoke(new Action(delegate {
                            barBroadcast.Setup(broadcastStatus.Content as Broadcast, onCustomLinkClick);
                            barBroadcast.Appear();
                        }));

                    bool IsBroadcastAvailable() => broadcastStatus.Code == Code.SUCCESS;
                };
            }
        }

        public void Setup(
            Func<bool> isNeedToShowPolicyWindow,
            Func<bool> shouldStartHidden,
            Func<bool> isNeedToAutoConnect,
            Func<Config> getConfig,
            Func<Status> loadConfig, 
            Func<Status> enableMode,
            Func<Status> checkForUpdate,
            Func<Status> checkForBroadcast,
            Func<ServerWindow> openServerWindow,
            Func<SettingsWindow> openSettingsWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Func<PolicyWindow> openPolicyWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onCancelServer,
            Action onDisableMode,
            Action onGenerateClientId,
            Action onGitHubClick,
            Action onBugReportingClick,
            Action<string> onCustomLinkClick)
        {
            this.isNeedToShowPolicyWindow = isNeedToShowPolicyWindow;
            this.shouldStartHidden = shouldStartHidden;
            this.isNeedToAutoConnect = isNeedToAutoConnect;
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.checkForBroadcast = checkForBroadcast;
            this.openServerWindow = openServerWindow;
            this.openSettingsWindow = openSettingsWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.openPolicyWindow = openPolicyWindow;
            this.onRunServer = onRunServer;
            this.onCancelServer = onCancelServer;
            this.onStopServer = onStopServer;
            this.enableMode = enableMode;
            this.onDisableMode = onDisableMode;
            this.onGenerateClientId = onGenerateClientId;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;
            this.onCustomLinkClick = onCustomLinkClick;

            InitializeConnectionPipeline();
            InitializeViewModelDelegates();

            UpdateUI();
        }

        /// <summary>
        /// Шаг 4 (завершение): прокидываем legacy-конвейер Core в ConnectionService
        /// (единая точка входа для MVVM), один раз за жизненный цикл окна.
        /// </summary>
        private void InitializeConnectionPipeline()
        {
            if (isConnectionPipelineInitialized)
                return;

            ServiceLocator.Get<ConnectionService>().Setup(
                loadConfig: loadConfig,
                enableMode: enableMode,
                runServer: onRunServer,
                stopServer: onStopServer,
                cancelServer: onCancelServer,
                disableMode: onDisableMode
            );

            isConnectionPipelineInitialized = true;
        }

        /// <summary>
        /// Шаг 4 (MVVM): создаём MainViewModel поверх ConnectionService (если ещё нет)
        /// и инициализируем его делегатами после Setup(). DataContext — для bindings в XAML.
        /// </summary>
        private void InitializeViewModel()
        {
            ConnectionService connectionService = ServiceLocator.Get<ConnectionService>();

            ViewModel = new MainViewModel(connectionService);
            DataContext = ViewModel;

            // Legacy-визуалы подписаны на состояние VM: единый источник истины.
            ViewModel.StateChangedExternally += (sender, state) => ApplyVisualState(state);
        }

        private void InitializeViewModelDelegates()
        {
            ViewModel.Initialize(
                getConfig: getConfig,
                openServerWindow: () => OpenServerWindow(),
                openSettingsWindow: () => OpenSettingsWindow(),
                openUpdateWindow: () => OpenUpdateWindow(),
                openAboutWindow: () => OpenAboutWindow(),
                onGitHubClick: () => onGitHubClick.Invoke(),
                onBugReportingClick: () => onBugReportingClick.Invoke()
            );
        }

        /// <summary>
        /// Мост ViewModel → legacy визуальное дерево.
        /// Шаг 4 (MVVM): видимость Hero-зоны полностью declarative (Binding State +
        /// ConnectionStateToVisibilityConverter в XAML), поэтому здесь остаётся
        /// только побочная логика смены состояния (анимации/фокус — по мере надобности).
        /// </summary>
        private void ApplyVisualState(MainViewModel.ConnectionState state)
        {
            // No-op: visibility управляется биндингами на ViewModel.State.
        }

        protected override void OnContentRendered(EventArgs e)
        {
            TryOpenPolicyWindow();
            TryStartHidden();
            TryAutoConnect();

            AnalyticsService.SendEvent(new AppOpenedEvent());
        }

        public void UpdateUI()
        {
            Config config = getConfig.Invoke();

            // Шаг 4 (MVVM): имя конфига живёт в VM — legacy-TextBlock остаётся как fallback.
            ViewModel?.RefreshConfigName();

            if (config == null)
            {
                textServerConfig.Text = LocalizationService.GetTerm(Localization.NO_SERVER_CONFIGURATION);
                return;
            }
            
            textServerConfig.Text = config.Name;
        }

        public void TryRerun()
        {
            // Шаг 4 (MVVM): делегируем в RestartCommand — ConnectionService остановит
            // worker и перезапустит его с новым конфигом (состояние остаётся Connecting/Connected).
            ViewModel?.RestartCommand.Execute(null);
        }

        public void TryDisableModeAndRerun()
        {
            // Шаг 4 (MVVM): делегируем в RestartWithDisabledModeCommand.
            ViewModel?.RestartWithDisabledModeCommand.Execute(null);
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
            AnalyticsService.SendEvent(new ManageServersButtonClickedEvent());
        }

        // NOTE (Шаг 4, MVVM): OnRunButtonClick/OnStopButtonClick/OnCancelButtonClick и
        // клик-обработчики footer-иконок удалены — XAML биндится на команды MainViewModel.






        private void TryStartHidden()
        {
            if (!shouldStartHidden.Invoke())
                return;

            if(ShouldAvoidStartHidden())
                return;
            
            OnClosing(new CancelEventArgs());

            bool ShouldAvoidStartHidden() => Application.Current.Windows.Count > 1;
        }

        private void TryAutoConnect()
        {
            if (!isNeedToAutoConnect.Invoke())
                return;

            // Шаг 4 (MVVM): автоподключение идёт через команду VM, как и клик по Hero-кнопке.
            ViewModel?.ConnectAsyncCommand.Execute(null);
        }

        private void TryOpenPolicyWindow()
        {
            if (!isNeedToShowPolicyWindow.Invoke())
                return;

            onGenerateClientId.Invoke();
            AnalyticsService.SendEvent(new NewUserEvent());

            PolicyWindow policyWindow = openPolicyWindow.Invoke();
            policyWindow.Owner = this;
            policyWindow.ShowDialog();
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
        }

        private void OpenSettingsWindow()
        {
            SettingsWindow settingsWindow = openSettingsWindow.Invoke();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void OpenUpdateWindow()
        {
            UpdateWindow updateWindow = openUpdateWindow.Invoke();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
        }

        private void OpenAboutWindow()
        {
            AboutWindow aboutWindow = openAboutWindow.Invoke();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        // NOTE (Шаг 4, MVVM): ShowRunStatus/ShowStopStatus/ShowWaitForRunStatus удалены —
        // видимость status*/button* теперь declarative-биндинги на ViewModel.State.

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
