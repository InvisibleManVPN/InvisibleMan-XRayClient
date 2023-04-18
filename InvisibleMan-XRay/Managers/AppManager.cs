using System.Windows;
using System.Threading;

namespace InvisibleManXRay.Managers
{
    using Models;
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
            handlersManager.AddHandler(new ServiceHandler());
            handlersManager.AddHandler(new ConfigHandler());
            handlersManager.AddHandler(new ProxyHandler());
            handlersManager.AddHandler(new TunnelHandler());
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
            SetupServiceHandler();
            SetupTunnelHandler();
            SetupConfigHandler();
            SetupNotifyHandler();

            void SetupServiceHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                handlersManager.GetHandler<ServiceHandler>().Setup(
                    getTunnelPort: settingsHandler.UserSettings.GetTunPort
                );
            }

            void SetupTunnelHandler()
            {
                ServiceHandler serviceHandler = handlersManager.GetHandler<ServiceHandler>();

                handlersManager.GetHandler<TunnelHandler>().Setup(
                    onStartTunnelingService: serviceHandler.TunnelService.Start,
                    isServiceRunning: serviceHandler.TunnelService.IsServiceRunning,
                    isServicePortActive: serviceHandler.TunnelService.IsServicePortActive,
                    connectTunnelingService: serviceHandler.TunnelService.Connect,
                    executeCommand: serviceHandler.TunnelService.Execute
                );
            }

            void SetupConfigHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<ConfigHandler>().Setup(
                    getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex
                );
            }

            void SetupNotifyHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<NotifyHandler>().Setup(
                    getMode: settingsHandler.UserSettings.GetMode,
                    onOpenClick: OpenApplication,
                    onUpdateClick: OpenUpdateWindow,
                    onAboutClick: OpenAboutWindow,
                    onCloseClick: CloseApplication,
                    onProxyModeClick: () => { OnModeClick(Mode.PROXY); },
                    onTunnelModeClick: () => { OnModeClick(Mode.TUN); }
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
                    core.DisableMode();
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

                void OnModeClick(Mode mode) => settingsHandler.UpdateMode(mode);
            }
        }

        private void SetupCore()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            ProxyHandler proxyHandler = handlersManager.GetHandler<ProxyHandler>();
            TunnelHandler tunnelHandler = handlersManager.GetHandler<TunnelHandler>();

            core.Setup(
                getConfig: configHandler.GetCurrentConfig,
                getMode: settingsHandler.UserSettings.GetMode,
                getTunIp: settingsHandler.UserSettings.GetTunIp,
                getDns: settingsHandler.UserSettings.GetDns,
                getProxy: proxyHandler.GetProxy,
                getTunnel: tunnelHandler.GetTunnel,
                onFailLoadingConfig: configHandler.RemoveConfigFromList
            );
        }

        private void SetupFactory()
        {
            WindowFactory.Setup(core, handlersManager);
        }
    }
}