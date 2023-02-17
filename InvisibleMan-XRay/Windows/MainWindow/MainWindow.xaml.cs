using System;
using System.Windows;

namespace InvisibleManXRay
{
    using Models;
    using Values;

    public partial class MainWindow : Window
    {
        private Func<Status> loadConfig;
        private Func<ServerWindow> openServerWindow;
        private Action<string> onRunServer;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Setup(
            Func<Status> loadConfig, 
            Func<ServerWindow> openServerWindow,
            Action<string> onRunServer)
        {
            this.loadConfig = loadConfig;
            this.openServerWindow = openServerWindow;
            this.onRunServer = onRunServer;
        }

        private void OnManageServersClick(object sender, RoutedEventArgs e)
        {
            OpenServerWindow();
        }

        private void OnConnectButtonClick(object sender, RoutedEventArgs e)
        {
            Status configStatus = loadConfig.Invoke();

            if (configStatus.Code == Code.ERROR)
            {
                HandleError();
                return;
            }

            onRunServer.Invoke(configStatus.Content);

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
        }

        private void OpenServerWindow()
        {
            ServerWindow serverWindow = openServerWindow.Invoke();
            serverWindow.Owner = this;
            serverWindow.ShowDialog();
        }
    }
}
