using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace InvisibleManXRay
{
    using Managers;
    using Services;
    using Windows;

    public partial class App : Application
    {
        private AppManager appManager;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            InitializeAppManager(
                onComplete: () => {
                    ShowMainWindow();
                    base.OnFrameworkInitializationCompleted();
                }
            );

            void InitializeAppManager(Action onComplete)
            {
                appManager = new AppManager();
                appManager.Initialize(onComplete);
            }

            void ShowMainWindow()
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.MainWindow = ServiceLocator.Find<WindowsService>().OpenWindow<MainWindow>();
            }
        }
    }
}