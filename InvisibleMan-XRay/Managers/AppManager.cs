using System.Windows;
using System.Threading;

namespace InvisibleManXRay.Managers
{
    using Core;
    using Handlers;
    using Factories;
    using Values;

    public class AppManager
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;
        public WindowFactory WindowFactory;

        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-S9I1-43bl-A0eM-72A47N6EDH8M}";

        public void Initialize()
        {
            AvoidRunningMultipleInstances();

            RegisterCore();
            RegisterHandlers();
            RegisterFactory();

            SetupHandlers();
            SetupCore();
            SetupFactory();
        }

        private void AvoidRunningMultipleInstances()
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            if(!isCreatedNew)
            {
                MessageBox.Show(Message.APP_ALREADY_RUNNING);
                Application.Current.Shutdown();
            }
        }

        private void RegisterCore()
        {
            core = new InvisibleManXRayCore();
        }

        private void RegisterHandlers()
        {
            handlersManager = new HandlersManager();

            handlersManager.AddHandler(new SettingsHandler());
            handlersManager.AddHandler(new TemplateHandler());
            handlersManager.AddHandler(new ConfigHandler());
            handlersManager.AddHandler(new ProxyHandler());
            handlersManager.AddHandler(new NotifyHandler());
            handlersManager.AddHandler(new UpdateHandler());
            handlersManager.AddHandler(new BroadcastHandler());
            handlersManager.AddHandler(new LinkHandler());
        }

        private void RegisterFactory()
        {
            WindowFactory = new WindowFactory();
        }

        private void SetupHandlers()
        {
            SetupConfigHandler();
            SetupNotifyHandler();

            void SetupConfigHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<ConfigHandler>().Setup(
                    getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex
                );
            }

            void SetupNotifyHandler()
            {
                handlersManager.GetHandler<NotifyHandler>().Setup(
                    onOpenClick: OpenApplication,
                    onUpdateClick: OpenUpdateWindow,
                    onAboutClick: OpenAboutWindow,
                    onCloseClick: CloseApplication
                );

                bool IsAnotherWindowOpened() => Application.Current.Windows.Count > 1;

                bool IsMainWindow(Window window) => window == Application.Current.MainWindow;

                void ShowMainWindow() => Application.Current.MainWindow.Show();

                void CloseOtherWindows()
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (!IsMainWindow(window))
                            window.Close();
                    }
                }

                void OpenApplication()
                {
                    ShowMainWindow();
                    Application.Current.MainWindow.WindowState = WindowState.Normal;
                }

                void CloseApplication()
                {
                    Application.Current.Shutdown();
                }
                
                void OpenUpdateWindow() 
                {
                    ShowMainWindow();
                    if(IsAnotherWindowOpened())
                        CloseOtherWindows();

                    UpdateWindow updateWindow = WindowFactory.CreateUpdateWindow();
                    updateWindow.Owner = Application.Current.MainWindow;
                    updateWindow.ShowDialog();
                }

                void OpenAboutWindow()
                {
                    ShowMainWindow();
                    if(IsAnotherWindowOpened())
                        CloseOtherWindows();

                    AboutWindow aboutWindow = WindowFactory.CreateAboutWindow();
                    aboutWindow.Owner = Application.Current.MainWindow;
                    aboutWindow.ShowDialog();
                }
            }
        }

        private void SetupCore()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            ProxyHandler proxyHandler = handlersManager.GetHandler<ProxyHandler>();

            core.Setup(
                getConfig: configHandler.GetCurrentConfig,
                getProxy: proxyHandler.GetProxy,
                onFailLoadingConfig: configHandler.RemoveConfigFromList
            );
        }

        private void SetupFactory()
        {
            WindowFactory.Setup(core, handlersManager);
        }
    }
}