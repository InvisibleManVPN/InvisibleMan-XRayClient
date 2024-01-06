using System;
using System.Collections.Generic;

namespace InvisibleManXRay.Managers
{
    using Services;

    public class ServicesManager
    {
        private Dictionary<Type, Service> services;

        public ServicesManager()
        {
            services = new Dictionary<Type, Service>();
        }

        public void AddService(Service service)
        {
            services.Add(service.GetType(), service);
        }

        public T GetService<T>() where T : Service
        {
            Type type = typeof(T);

            if (IsServiceExists(type))
                return services[type] as T;
            throw new Exception($"The service of type {type} does not found.");

            bool IsServiceExists(Type type) => services.ContainsKey(type);
        }
    }
}