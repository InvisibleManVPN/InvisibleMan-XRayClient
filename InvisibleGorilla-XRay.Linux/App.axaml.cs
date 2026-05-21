using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace InvisibleGorillaXRay.Linux
{
    using Managers;

    public partial class App : Application
    {
        private LinuxAppManager? appManager;
        private bool _cleanedUp;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                appManager = new LinuxAppManager(desktop.Args ?? Array.Empty<string>());
                appManager.Initialize();

                var mainWindow = appManager.WindowFactory.CreateMainWindow();
                desktop.MainWindow = mainWindow;

                desktop.ShutdownRequested += (s, e) => CleanupBeforeExit();
                AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupBeforeExit();
                AppDomain.CurrentDomain.UnhandledException += (s, e) => CleanupBeforeExit();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void CleanupBeforeExit()
        {
            if (_cleanedUp) return;
            _cleanedUp = true;

            try { appManager?.Core?.DisableMode(); } catch { }
        }
    }
}
