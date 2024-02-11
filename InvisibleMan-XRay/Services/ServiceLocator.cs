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

        public static T Find<T>() where T : IService => ServicesManager.GetService<T>();
    }
}