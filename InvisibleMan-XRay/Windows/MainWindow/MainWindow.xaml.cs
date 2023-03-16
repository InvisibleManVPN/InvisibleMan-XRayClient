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
        private Func<Status> checkForUpdate;
        private Func<ServerWindow> openServerWindow;
        private Func<UpdateWindow> openUpdateWindow;
        private Func<AboutWindow> openAboutWindow;
        private Action<string> onRunServer;
        private Action onStopServer;
        private Action onEnableProxy;
        private Action onDisableProxy;
        private Action onGitHubClick;
        private Action onBugReportingClick;

        private BackgroundWorker connectWorker;
        private BackgroundWorker updateWorker;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnectWorker();
            InitializeUpdateWorker();
            updateWorker.RunWorkerAsync();

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

                    onEnableProxy.Invoke();

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

                updateWorker.DoWork+=(sender, e) => {
                    Status updateStatus = checkForUpdate.Invoke();
                    if (IsUpdateAvailable())
                        Dispatcher.BeginInvoke(new Action(delegate {
                            notificationUpdate.Visibility = Visibility.Visible;
                        }));

                    bool IsUpdateAvailable() => updateStatus.SubCode == SubCode.UPDATE_AVAILABLE;
                };
            }
        }

        public void Setup(
            Func<Config> getConfig,
            Func<Status> loadConfig, 
            Func<Status> checkForUpdate,
            Func<ServerWindow> openServerWindow,
            Func<UpdateWindow> openUpdateWindow,
            Func<AboutWindow> openAboutWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onEnableProxy,
            Action onDisableProxy,
            Action onGitHubClick,
            Action onBugReportingClick)
        {
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.checkForUpdate = checkForUpdate;
            this.openServerWindow = openServerWindow;
            this.openUpdateWindow = openUpdateWindow;
            this.openAboutWindow = openAboutWindow;
            this.onRunServer = onRunServer;
            this.onStopServer = onStopServer;
            this.onEnableProxy = onEnableProxy;
            this.onDisableProxy = onDisableProxy;
            this.onGitHubClick = onGitHubClick;
            this.onBugReportingClick = onBugReportingClick;

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
            onDisableProxy.Invoke();
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
