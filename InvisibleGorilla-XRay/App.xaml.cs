using System;
using System.Windows;
using Microsoft.Win32;

namespace InvisibleGorillaXRay
{
    using Managers;
    using Handlers;
    using Factories;

    public partial class App : Application
    {
        private AppManager appManager;
        private WindowFactory windowFactory;

        private EventHandler processExitHandler;
        private SessionEndedEventHandler sessionEndedHandler;

        protected override void OnStartup(StartupEventArgs e)
        {
            InitializeAppManager();
            InitializeNotifyIcon();
            InitializeWindowFactory();
            InitializeMainWindow();
            HandlePipes();
            HandleExitingEvents();

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

            void HandleExitingEvents()
            {
                processExitHandler = (sender, args) => CleanupBeforeExit();
                sessionEndedHandler = (sender, args) => CleanupBeforeExit();

                AppDomain.CurrentDomain.ProcessExit += processExitHandler;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                SystemEvents.SessionEnded += sessionEndedHandler;
            }

            bool IsThereAnyArg() => e.Args.Length != 0;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CleanupBeforeExit();

            AppDomain.CurrentDomain.ProcessExit -= processExitHandler;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            SystemEvents.SessionEnded -= sessionEndedHandler;

            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CleanupBeforeExit();
        }

        private void CleanupBeforeExit()
        {
            try
            {
                appManager?.Core?.Stop();
            }
            catch { }

            try
            {
                appManager?.Core?.DisableMode();
            }
            catch { }
        }
    }
}
