using System.Windows;

namespace InvisibleManXRay
{
    using Managers;
    using Handlers;
    using Factories;

    public partial class App : Application
    {
        private AppManager appManager;
        private WindowFactory windowFactory;

        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeAppManager();
            InitializeNotifyIcon();
            InitializeWindowFactory();
            InitializeMainWindow();
            HandlePipes();

            void InitializeAppManager()
            {
                appManager = new AppManager(e.Args);
                appManager.Initialize();
            }

            void InitializeNotifyIcon()
            {
                appManager.HandlersManager.GetHandler<NotifyHandler>().InitializeNotifyIcon();
            }

            void InitializeWindowFactory()
            {
                windowFactory = appManager.WindowFactory;
            }

            void InitializeMainWindow()
            {
                MainWindow mainWindow = windowFactory.CreateMainWindow();
                mainWindow.Show();
            }

            void HandlePipes()
            {
                if (IsThereAnyArg())
                    PipeManager.SignalThisApp(e.Args);
                
                PipeManager.ListenForPipes();
            }

            bool IsThereAnyArg() => e.Args.Length != 0;
        }
    }
}
