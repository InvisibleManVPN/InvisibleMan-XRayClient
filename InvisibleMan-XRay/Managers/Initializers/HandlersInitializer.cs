namespace InvisibleManXRay.Managers.Initializers
{
    using Handlers;

    public class HandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler());
            HandlersManager.AddHandler(new ThemeHandler());
            HandlersManager.AddHandler(new LocalizationHandler());
        }

        public void Setup()
        {
            SettingsHandler settingsHandler = HandlersManager.GetHandler<SettingsHandler>();
            SetupThemeHandler();
            SetupLocalizationHandler();

            void SetupThemeHandler()
            {
                HandlersManager.GetHandler<ThemeHandler>().Setup(
                    getCurrentTheme: () => settingsHandler.GetSettingsData().Theme
                );
            }

            void SetupLocalizationHandler()
            {
                HandlersManager.GetHandler<LocalizationHandler>().Setup(
                    getCurrentLanguage: () => settingsHandler.GetSettingsData().Language
                );
            }
        }
    }
}