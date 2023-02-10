namespace InvisibleManXRay.Managers
{
    using Core;
    using Handlers;

    public class AppManager
    {
        public InvisibleManXRayCore Core { get; private set; }
        public HandlersManager HandlersManager { get; private set; }
        public static AppManager Instance { get; private set; }

        public void Initialize()
        {
            if (Instance != null)
                return;
            
            Instance = this;
        }

        private void RegisterCore()
        {
            Core = new InvisibleManXRayCore();
        }

        private void RegisterHandlers()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler());
        }
    }
}