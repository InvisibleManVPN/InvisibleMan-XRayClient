namespace InvisibleManXRay.Services
{
    using Managers;

    public static class ServiceLocator
    {
        private static ServicesManager ServicesManager;

        public static void Setup(ServicesManager servicesManager)
        {
            ServicesManager = servicesManager;
        }

        public static T Get<T>() where T : Service => ServicesManager.GetService<T>();
    }
}