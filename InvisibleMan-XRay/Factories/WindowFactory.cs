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
                openUpdateWindow: CreateUpdateWindow,
                openAboutWindow: CreateAboutWindow,
                onRunServer: core.Run,
                onStopServer: core.Stop,
                onEnableProxy: core.EnableProxy,
                onDisableProxy: core.DisableProxy
            );
            
            return mainWindow;
        }

        public UpdateWindow CreateUpdateWindow()
        {
            UpdateHandler updateHandler = handlersManager.GetHandler<UpdateHandler>();

            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.Setup(
                checkForUpdate: updateHandler.CheckForUpdate,
                onUpdateClick: updateHandler.OpenUpdateWebPage
            );

            return updateWindow;
        }

        public AboutWindow CreateAboutWindow()
        {
            AboutWindow aboutWindow = new AboutWindow();
            return aboutWindow;
        }

        private ServerWindow CreateServerWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            TemplateHandler templateHandler = handlersManager.GetHandler<TemplateHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            
            ServerWindow serverWindow = new ServerWindow();
            serverWindow.Setup(
                getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex,
                getAllConfigs: configHandler.GetAllConfigs,
                convertConfigLinkToV2Ray: templateHandler.ConverLinkToV2Ray,
                loadConfig: core.LoadConfig,
                testConnection: core.Test,
                onCopyConfig: configHandler.CopyConfig,
                onCreateConfig: configHandler.CreateConfig,
                onDeleteConfig: configHandler.LoadConfigFiles,
                onUpdateConfigIndex: settingsHandler.UpdateCurrentConfigIndex
            );
            
            return serverWindow;
        }
    }
}