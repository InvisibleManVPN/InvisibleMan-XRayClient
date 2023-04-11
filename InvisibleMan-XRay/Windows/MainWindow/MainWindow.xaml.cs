using System;
using System.Windows;
using System.ComponentModel;

namespace InvisibleManXRay
{
    using Models;
    using Values;

    public partial class MainWindow : Window
    {
        private bool isReconnectingRequest;

        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Func<Status> checkForUpdate;
        private Func<Status> checkForBroadcast;
        private Func<ServerWindow> openServerWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Action<string> onRunServer;
        private Action onStopServer;
        private Action onDisableMode;
        private Action onGitHubClick;
        private Action onBugReportingClick;
        private Action<string> onCustomLinkClick;

        private BackgroundWorker connectWorker;
        private BackgroundWorker updateWorker;
        private BackgroundWorker broadcastWorker;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnectWorker();
            InitializeUpdateWorker();
            InitializeBroadcastWorker();

            updateWorker.RunWorkerAsync();
            broadcastWorker.RunWorkerAsync();

            void InitializeConnectWorker()
            {
                connectWorker = new BackgroundWorker();

                connectWorker.RunWorkerCompleted += (sender, e) => {
                    if (isReconnectingRequest)
                    {
                        connectWorker.RunWorkerAsync();
                        isReconnectingRequest = false;
                    }
                };

                connectWorker.DoWork += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowConnectingStatus();
                    }));

                    Status configStatus = loadConfig.Invoke();

                    if (configStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            HandleError();
                            ShowDisconnectStatus();
                        }));

                        return;
                    }

                    Status modeStatus = enableMode.Invoke();

                    if (modeStatus.Code == Code.ERROR)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate {
                            MessageBox.Show(
                                modeStatus.Content.ToString(), 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                            ShowDisconnectStatus();
                        }));
                        
                        return;
                    }

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowConnectStatus();
                    }));

                    onRunServer.Invoke(configStatus.Content.ToString());

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowDisconnectStatus();
                    }));

                    void HandleError()
                    {
                        if (IsAnotherWindowOpened())
                            return;

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

                        bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

                        void HandleNoConfigError()
                        {
                            MessageBoxResult result = MessageBox.Show(
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
            Func<Config> getConfig,
            Func<Status> loadConfig, 
            Func<Status> enableMode,
            Func<Status> checkForUpdate,
            Func<Status> checkForBroadcast,
            Func<ServerWindow> openServerWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onDisableMode,
            Action onGitHubClick,
            Action onBugReportingClick,
            Action<string> onCustomLinkClick)
        {
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.checkForBroadcast = checkForBroadcast;
            this.openServerWindow = openServerWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.onRunServer = onRunServer;
            this.onStopServer = onStopServer;
            this.enableMode = enableMode;
            this.onDisableMode = onDisableMode;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;
            this.onCustomLinkClick = onCustomLinkClick;

            UpdateUI();
        }

        public void UpdateUI()
        {
            Config config = getConfig.Invoke();

            if (config == null)
            {
                textServerConfig.Content = Message.NO_SERVER_CONFIGURATION;
                return;
            }
            
            textServerConfig.Content = config.Name;
        }

        public void TryReconnect()
        {
            if (!connectWorker.IsBusy)
                return;
            
            onStopServer.Invoke();
            isReconnectingRequest = true;
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
        }

        private void OnConnectButtonClick(object sender, RoutedEventArgs e)
        {
            if (connectWorker.IsBusy)
                return;

            connectWorker.RunWorkerAsync();
        }

        private void OnDisconnectButtonClick(object sender, RoutedEventArgs e)
        {
            onStopServer.Invoke();
            onDisableMode.Invoke();
            isReconnectingRequest = false;
        }

        private void OnGitHubButtonClick(object sender, RoutedEventArgs e)
        {
            onGitHubClick.Invoke();
        }

        private void OnBugReportingButtonClick(object sender, RoutedEventArgs e)
        {
            onBugReportingClick.Invoke();
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            OpenUpdateWindow();
        }

        private void OnAboutButtonClick(object sender, RoutedEventArgs e)
        {
            OpenAboutWindow();
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
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

        private void ShowConnectStatus()
        {
            statusConnect.Visibility = Visibility.Visible;
            statusDisconnect.Visibility = Visibility.Hidden;
            statusConnecting.Visibility = Visibility.Hidden;

            buttonDisconnect.Visibility = Visibility.Visible;
            buttonConnect.Visibility = Visibility.Hidden;
        }

        private void ShowDisconnectStatus()
        {
            statusDisconnect.Visibility = Visibility.Visible;
            statusConnect.Visibility = Visibility.Hidden;
            statusConnecting.Visibility = Visibility.Hidden;

            buttonConnect.Visibility = Visibility.Visible;
            buttonDisconnect.Visibility = Visibility.Hidden;
        }

        private void ShowConnectingStatus()
        {
            statusConnecting.Visibility = Visibility.Visible;
            statusDisconnect.Visibility = Visibility.Hidden;
            statusConnect.Visibility = Visibility.Hidden;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
