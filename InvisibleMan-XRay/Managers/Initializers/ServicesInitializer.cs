namespace InvisibleManXRay.Managers.Initializers
{
    using Factories;
    using Handlers;
    using Services;

    public class ServicesInitializer
    {
        public ServicesManager ServicesManager { get; private set; }

        public void Register()
        {
            ServicesManager = new ServicesManager();

            ServicesManager.AddService(new LocalizationService());
            ServicesManager.AddService(new WindowsService());
        }

        public void Setup(
            HandlersManager handlersManager,
            WindowsFactory windowsFactory
        )
        {
            SetupServiceLocator();
            SetupLocalizationService();
            SetupWindowsService();

            void SetupServiceLocator()
            {
                ServiceLocator.Setup(ServicesManager);
            }

            void SetupLocalizationService()
            {
                ServicesManager.GetService<LocalizationService>().Setup(
                    getLocalizationResource: handlersManager.GetHandler<LocalizationHandler>().GetLocalizationResource
                );
            }

            void SetupWindowsService()
            {
                ServicesManager.GetService<WindowsService>().Setup(windowsFactory);
            }
        }
    }
}