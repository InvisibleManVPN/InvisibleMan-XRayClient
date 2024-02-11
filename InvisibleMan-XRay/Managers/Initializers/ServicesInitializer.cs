namespace InvisibleManXRay.Managers.Initializers
{
    using Factories;
    using Services;

    public class ServicesInitializer
    {
        public ServicesManager ServicesManager { get; private set; }

        public void Register()
        {
            ServicesManager = new ServicesManager();
            ServicesManager.AddService(new WindowsService());
        }

        public void Setup(WindowsFactory windowsFactory)
        {
            SetupServiceLocator();
            SetupWindowsService();

            void SetupServiceLocator()
            {
                ServiceLocator.Setup(ServicesManager);
            }

            void SetupWindowsService()
            {
                ServicesManager.GetService<WindowsService>().Setup(windowsFactory);
            }
        }
    }
}