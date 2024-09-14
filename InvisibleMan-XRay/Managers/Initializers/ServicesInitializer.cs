namespace InvisibleManXRay.Managers.Initializers
{
    using Models;
    using Managers;
    using Services;
    using Handlers;

    public class ServicesInitializer
    {
        public ServicesManager ServicesManager { get; private set; }

        public void Register()
        {
            ServicesManager = new ServicesManager();

            ServicesManager.AddService(new LocalizationService());
            ServicesManager.AddService(new AnalyticsService());
        }

        public void Setup(HandlersManager handlersManager)
        {
            SetupServiceLocator();
            SetupLocalizationService();
            SetupAnalyticsService();

            void SetupServiceLocator()
            {
                ServiceLocator.Setup(
                    servicesManager: ServicesManager
                );
            }

            void SetupLocalizationService()
            {
                ServicesManager.GetService<LocalizationService>().Setup(
                    getLocalizationResource: handlersManager.GetHandler<LocalizationHandler>().GetLocalizationResource
                );
            }

            void SetupAnalyticsService()
            {
                SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                VersionHandler versionHandler = handlersManager.GetHandler<VersionHandler>();

                ServicesManager.GetService<AnalyticsService>().Setup(
                    getClientId: settingsHandler.UserSettings.GetClientId,
                    getSendingAnalyticsEnabled: settingsHandler.UserSettings.GetSendingAnalyticsEnabled,
                    getApplicationVersion: GetApplicationVersion
                );

                string GetApplicationVersion()
                {
                    AppVersion appVersion = versionHandler.GetApplicationVersion();
                    return $"{appVersion.Major}.{appVersion.Feature}.{appVersion.BugFix}";
                }
            }
        }
    }
}