namespace InvisibleManXRay.Managers
{
    using Foundation;
    using Services;

    public class ServicesManager
    {
        private readonly Container<IService> services;
        private const string TAG = "service";

        public ServicesManager()
        {
            this.services = new Container<IService>(TAG);
        }

        public void AddService(IService service) => services.Add(service);

        public T GetService<T>() where T : IService => services.Get<T>();
    }
}