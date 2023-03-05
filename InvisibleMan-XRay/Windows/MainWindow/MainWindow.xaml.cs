using System;
using System.Windows;
using System.ComponentModel;

namespace InvisibleManXRay
{
    using Models;
    using Values;

    public partial class MainWindow : Window
    {
        private Func<Config> getConfig;
        private Func<Status> loadConfig;
        private Func<ServerWindow> openServerWindow;
        private Action<string> onRunServer;
        private Action onStopServer;
        private Action onEnableProxy;
        private Action onDisableProxy;

        private BackgroundWorker connectWorker;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnectWorker();

            void InitializeConnectWorker()
            {
                connectWorker = new BackgroundWorker();

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

                    onRunServer.Invoke(configStatus.Content);

                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowDisconnectStatus();
                    }));

                    void HandleError()
                    {
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

                        void HandleNoConfigError()
                        {
                            MessageBoxResult result = MessageBox.Show(
                                configStatus.Content, 
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
                                configStatus.Content, 
                                Caption.ERROR, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error
                            );
                        }
                    }
                };
            }
        }

        public void Setup(
            Func<Config> getConfig,
            Func<Status> loadConfig, 
            Func<ServerWindow> openServerWindow,
            Action<string> onRunServer,
            Action onStopServer,
            Action onEnableProxy,
            Action onDisableProxy)
        {
            this.getConfig = getConfig;
            this.loadConfig = loadConfig;
            this.openServerWindow = openServerWindow;
            this.onRunServer = onRunServer;
            this.onStopServer = onStopServer;
            this.onEnableProxy = onEnableProxy;
            this.onDisableProxy = onDisableProxy;

            UpdateUI();
        }

        private void UpdateUI()
        {
            Config config = getConfig.Invoke();

            if (config == null)
            {
                textServerConfig.Content = Message.NO_SERVER_CONFIGURATION;
                return;
            }
            
            textServerConfig.Content = config.Name;
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
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
            UpdateUI();
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
