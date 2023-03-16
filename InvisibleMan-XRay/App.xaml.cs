using System.Windows;

namespace InvisibleManXRay
{
    using Managers;
    using Factories;

    public partial class App : Application
    {
        private AppManager appManager;
        private WindowFactory windowFactory;

        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeAppManager();
            InitializeWindowFactory();
            InitializeMainWindow();

            void InitializeAppManager()
            {
                appManager = new AppManager();
                appManager.Initialize();
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
        }
    }
}
