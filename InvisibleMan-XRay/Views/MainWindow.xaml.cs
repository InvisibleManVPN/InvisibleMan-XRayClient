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
        private bool isRerunRequest;

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

        private BackgroundWorker runWorker;
        private BackgroundWorker updateWorker;
        private BackgroundWorker broadcastWorker;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
            InitializeNavigation();
            InitializeRunWorker();
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

            void InitializeRunWorker()
            {
                runWorker = new BackgroundWorker();

                runWorker.RunWorkerCompleted += (sender, e) => {
                    if (isRerunRequest)
                    {
                        runWorker.RunWorkerAsync();
                        isRerunRequest = false;
                    }
                };

                runWorker.DoWork += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ViewModel.State = MainViewModel.ConnectionState.Connecting;
                    }));

                    Status configStatus = loadConfig.Invoke();

                    if (configStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            HandleError();
                            ViewModel.State = MainViewModel.ConnectionState.Disconnected;
                        }));

                        return;
                    }

                    Status modeStatus = enableMode.Invoke();

                    if (modeStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            MessageBox.Show(
                                this,
                                modeStatus.Content.ToString(), 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                            ViewModel.State = MainViewModel.ConnectionState.Disconnected;
                        }));

                        return;
                    }
                    else if (modeStatus.Code == Code.INFO)
                    {
                        if (modeStatus.SubCode == SubCode.CANCELED)
                        {
                            Dispatcher.BeginInvoke(new Action(delegate {
                                ViewModel.State = MainViewModel.ConnectionState.Disconnected;
                            }));

                            return;
                        }
                    }

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ViewModel.State = MainViewModel.ConnectionState.Connected;
                    }));

                    onRunServer.Invoke(configStatus.Content.ToString());

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ViewModel.State = MainViewModel.ConnectionState.Disconnected;
                    }));

                    void HandleError()
                    {
                        if (IsAnotherWindowOpened())
                            return;
                        
                        ForceShowWindowIfNeeded();

                        switch (configStatus.SubCode)
                        {
                            case SubCode.NO_CONFIG:
                                HandleNoConfigError();
                                break;
                            case SubCode.INVALID_CONFIG:
                                HandleInvalidConfigError();
                                break;
                            default:
                                return;
                        }

                        bool IsWindowHidden() => this.Visibility == Visibility.Hidden;

                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

                        void ForceShowWindowIfNeeded()
                        {
                            if (!IsWindowHidden())
                                return;
                            
                            this.Show();
                        }

                        void HandleNoConfigError()
                        {
                            MessageBoxResult result = MessageBox.Show(
                                this,
                                configStatus.Content.ToString(), 
                                Caption.WARNING, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning
                            );

                            if (result == MessageBoxResult.OK)
                                OpenServerWindow();
                        }

                        void HandleInvalidConfigError()
                        {
                            MessageBox.Show(
                                this,
                                configStatus.Content.ToString(), 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                        }
                    }
                };
            }

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

            InitializeViewModelDelegates();

            ConnectionService connectionService = ServiceLocator.Get<ConnectionService>();
            connectionService.Setup(
                loadConfig: loadConfig,
                enableMode: enableMode,
                runServer: onRunServer,
                stopServer: onStopServer,
                cancelServer: onCancelServer,
                disableMode: onDisableMode);

            UpdateUI();
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
            // Шаг 4 (MVVM): делегируем в RestartCommand (connectionService.Restart).
            if (!runWorker.IsBusy)
                return;

            ViewModel?.RestartCommand.Execute(null);
        }

        public void TryDisableModeAndRerun()
        {
            // Шаг 4 (MVVM): делегируем в RestartWithDisabledModeCommand.
            if (!runWorker.IsBusy)
                return;

            ViewModel?.RestartWithDisabledModeCommand.Execute(null);
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
            AnalyticsService.SendEvent(new ManageServersButtonClickedEvent());
        }

        // NOTE (Шаг 4, MVVM): OnRunButtonClick/OnStopButtonClick/OnCancelButtonClick удалены —
        // XAML биндится напрямую на ToggleConnectionCommand (единая точка входа Hero-кнопки).

        private void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            onGitHubClick.Invoke();
            AnalyticsService.SendEvent(new GitHubButtonClickedEvent());
        }

        private void OnBugReportingButtonClick(object sender, RoutedEventArgs e)
        {
            onBugReportingClick.Invoke();
            AnalyticsService.SendEvent(new BugReportingButtonClickedEvent());
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsWindow();
            AnalyticsService.SendEvent(new SettingsButtonClickedEvent());
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            OpenUpdateWindow();
            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        private void OnAboutButtonClick(object sender, RoutedEventArgs e)
        {
            OpenAboutWindow();
            AnalyticsService.SendEvent(new AboutButtonClickedEvent());
        }

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
