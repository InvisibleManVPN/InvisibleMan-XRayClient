using System.Windows;

namespace InvisibleManXRay
{
    using Managers;

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeAppManager();
            InitializeMainWindow();

            void InitializeAppManager()
            {
                AppManager appManager = new AppManager();
                appManager.Initialize();
            }

            void InitializeMainWindow()
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
    }
}
