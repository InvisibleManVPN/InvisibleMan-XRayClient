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
        }

        private void RegisterFactory()
        {
            WindowFactory = new WindowFactory();
        }

        private void SetupHandlers()
        {
            handlersManager.GetHandler<ConfigHandler>().Setup(
                getConfigSettings: handlersManager.GetHandler<SettingsHandler>().GetConfigSettings
            );
        }

        private void SetupCore()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();

            core.Setup(
                getConfig: configHandler.GetCurrentConfig
            );
        }

        private void SetupFactory()
        {
            WindowFactory.Setup(core, handlersManager);
        }
    }
}