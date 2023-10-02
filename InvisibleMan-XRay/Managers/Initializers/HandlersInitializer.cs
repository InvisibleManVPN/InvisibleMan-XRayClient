using System.Windows;

namespace InvisibleManXRay.Managers.Initializers
{
    using Models;
    using Core;
    using Managers;
    using Handlers;
    using Factories;

    public class HandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler());
            HandlersManager.AddHandler(new TemplateHandler());
            HandlersManager.AddHandler(new ProcessHandler());
            HandlersManager.AddHandler(new DatabaseHandler());
            HandlersManager.AddHandler(new ConfigHandler());
            HandlersManager.AddHandler(new ProxyHandler());
            HandlersManager.AddHandler(new TunnelHandler());
            HandlersManager.AddHandler(new NotifyHandler());
            HandlersManager.AddHandler(new VersionHandler());
            HandlersManager.AddHandler(new UpdateHandler());
            HandlersManager.AddHandler(new BroadcastHandler());
            HandlersManager.AddHandler(new LinkHandler());
        }

        public void Setup(
            InvisibleManXRayCore core, 
            HandlersManager handlersManager, 
            WindowFactory windowFactory
        )
        {
            SetupProcessHandler();
            SetupTunnelHandler();
            SetupConfigHandler();
            SetupUpdateHandler();
            SetupNotifyHandler();

            void SetupProcessHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                handlersManager.GetHandler<ProcessHandler>().Setup(
                    getTunnelPort: settingsHandler.UserSettings.GetTunPort
                );
            }

            void SetupTunnelHandler()
            {
                ProcessHandler processHandler = handlersManager.GetHandler<ProcessHandler>();

                handlersManager.GetHandler<TunnelHandler>().Setup(
                    onStartTunnelingService: processHandler.TunnelProcess.Start,
                    isServiceRunning: processHandler.TunnelProcess.IsProcessRunning,
                    isServicePortActive: processHandler.TunnelProcess.IsProcessPortActive,
                    connectTunnelingService: processHandler.TunnelProcess.Connect,
                    executeCommand: processHandler.TunnelProcess.Execute
                );
            }

            void SetupConfigHandler()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

                handlersManager.GetHandler<ConfigHandler>().Setup(
                    getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath
                );
            }

            void SetupUpdateHandler()
            {
                VersionHandler versionHandler = handlersManager.GetHandler<VersionHandler>();

                handlersManager.GetHandler<UpdateHandler>().Setup(
                    getApplicationVersion: versionHandler.GetApplicationVersion,
                    convertToAppVersion: versionHandler.ConvertToAppVersion
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

                    UpdateWindow updateWindow = windowFactory.CreateUpdateWindow();
                    updateWindow.Owner = Application.Current.MainWindow;
                    updateWindow.ShowDialog();
                }

                void OpenAboutWindow()
                {
                    ShowMainWindow();
                    if(IsAnotherWindowOpened())
                        CloseOtherWindows();

                    AboutWindow aboutWindow = windowFactory.CreateAboutWindow();
                    aboutWindow.Owner = Application.Current.MainWindow;
                    aboutWindow.ShowDialog();
                }

                void OnModeClick(Mode mode) 
                {
                    if (mode == settingsHandler.UserSettings.GetMode())
                        return;

                    MainWindow mainWindow = windowFactory.GetMainWindow();
                    settingsHandler.UpdateMode(mode);
                    mainWindow.TryDisableModeAndRerun();
                }
            }
        }
    }
}