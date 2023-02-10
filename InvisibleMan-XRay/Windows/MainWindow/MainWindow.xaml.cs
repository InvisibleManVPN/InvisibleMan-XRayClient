using System;
using System.Windows;

namespace InvisibleManXRay
{
    using Models;
    using Values;

    public partial class MainWindow : Window
    {
        private Func<Status> loadConfig;
        private Action<string> onRunServer;
        private Func<ServerWindow> openServerWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Setup(
            Func<Status> loadConfig, 
            Action<string> onRunServer, 
            Func<ServerWindow> openServerWindow)
        {
            this.loadConfig = loadConfig;
            this.onRunServer = onRunServer;
            this.openServerWindow = openServerWindow;
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
                    {
                        ServerWindow serverWindow = openServerWindow.Invoke();
                        serverWindow.Owner = this;
                        serverWindow.ShowDialog();
                    }
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
    }
}
