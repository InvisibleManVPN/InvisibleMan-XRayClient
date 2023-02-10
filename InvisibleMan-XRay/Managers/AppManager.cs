namespace InvisibleManXRay.Managers
{
    using Core;
    using Handlers;

    public class AppManager
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;

        public void Initialize()
        {
            RegisterCore();
            RegisterHandlers();
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
    }
}