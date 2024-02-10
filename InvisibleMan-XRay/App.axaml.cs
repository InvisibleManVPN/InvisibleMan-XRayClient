using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace InvisibleManXRay
{
    using Managers;

    public partial class App : Application
    {
        private AppManager appManager;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            InitializeAppManager();
            base.OnFrameworkInitializationCompleted();

            void InitializeAppManager()
            {
                appManager = new AppManager();
                appManager.Initialize();
            }

            void ShowMainWindow()
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.MainWindow = new MainWindow();
            }
        }
    }
}