using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace InvisibleGorillaXRay.Mac
{
    using Core;
    using Managers;
    using Views;

    public partial class App : Application
    {
        private MacAppManager? appManager;
        private bool _cleanedUp;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    appManager = new MacAppManager(desktop.Args ?? Array.Empty<string>());
                    appManager.Initialize();

                    var mainWindow = appManager.WindowFactory.CreateMainWindow();
                    desktop.MainWindow = mainWindow;

                    desktop.ShutdownRequested += (s, e) => CleanupBeforeExit();
                    AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupBeforeExit();
                    AppDomain.CurrentDomain.UnhandledException += (s, e) => CleanupBeforeExit();
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("MacApp.OnFrameworkInitializationCompleted", ex);
                throw;
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
