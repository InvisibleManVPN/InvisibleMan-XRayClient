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
        }

        private void RegisterFactory()
        {
            WindowFactory = new WindowFactory();
        }

        private void SetupHandlers()
        {
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

            handlersManager.GetHandler<ConfigHandler>().Setup(
                getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex
            );
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