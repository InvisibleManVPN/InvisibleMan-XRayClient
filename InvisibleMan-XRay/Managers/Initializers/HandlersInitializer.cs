namespace InvisibleManXRay.Managers.Initializers
{
    using Handlers;

    public class HandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();
        }

        public void RegisterRootHandlers()
        {
            HandlersManager.AddHandler(new SettingsHandler());
            HandlersManager.AddHandler(new LocalizationHandler());
        }

        public void SetupRootHandlers()
        {
            SettingsHandler settingsHandler = HandlersManager.GetHandler<SettingsHandler>();

            HandlersManager.GetHandler<LocalizationHandler>().Setup(
                getCurrentLanguage: () => settingsHandler.GetSettingsData().Language
            );
        }
    }
}