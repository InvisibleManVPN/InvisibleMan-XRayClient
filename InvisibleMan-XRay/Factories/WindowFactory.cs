namespace InvisibleManXRay.Factories
{
    using Core;
    using Managers;
    using Handlers;

    public class WindowFactory
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;

        public void Setup(InvisibleManXRayCore core, HandlersManager handlersManager)
        {
            this.core = core;
            this.handlersManager = handlersManager;
        }

        public MainWindow CreateMainWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Setup(
                getConfig: configHandler.GetCurrentConfig,
                loadConfig: core.LoadConfig,
                openServerWindow: CreateServerWindow,
                onRunServer: core.Run,
                onStopServer: core.Stop,
                onEnableProxy: core.EnableProxy,
                onDisableProxy: core.DisableProxy
            );
            
            return mainWindow;
        }

        private ServerWindow CreateServerWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            
            ServerWindow serverWindow = new ServerWindow();
            serverWindow.Setup(
                getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex,
                getAllConfigs: configHandler.GetAllConfigs,
                loadConfig: core.LoadConfig,
                testConnection: core.Test,
                onAddConfig: configHandler.AddConfig,
                onDeleteConfig: configHandler.LoadConfigFiles,
                onUpdateConfigIndex: settingsHandler.UpdateCurrentConfigIndex
            );
            
            return serverWindow;
        }
    }
}