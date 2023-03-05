using System.Windows;

namespace InvisibleManXRay.Managers
{
    using Core;
    using Handlers;
    using Factories;

    public class AppManager
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;
        public WindowFactory WindowFactory;

        public void Initialize()
        {
            RegisterCore();
            RegisterHandlers();
            RegisterFactory();

            SetupHandlers();
            SetupCore();
            SetupFactory();
        }

        private void RegisterCore()
        {
            core = new InvisibleManXRayCore();
        }

        private void RegisterHandlers()
        {
            handlersManager = new HandlersManager();

            handlersManager.AddHandler(new SettingsHandler());
            handlersManager.AddHandler(new ConfigHandler());
            handlersManager.AddHandler(new ProxyHandler());
            handlersManager.AddHandler(new NotifyHandler());
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