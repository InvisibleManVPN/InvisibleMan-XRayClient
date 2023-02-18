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
            MainWindow mainWindow = new MainWindow();
            mainWindow.Setup(
                loadConfig: core.LoadConfig,
                openServerWindow: CreateServerWindow,
                onRunServer: core.Run
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
                onAddConfig: configHandler.AddConfig,
                onDeleteConfig: configHandler.LoadConfigFiles,
                onUpdateConfigIndex: settingsHandler.UpdateCurrentConfigIndex
            );
            
            return serverWindow;
        }
    }
}