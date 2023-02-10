using System;
using System.Collections.Generic;

namespace InvisibleManXRay.Managers
{
    using Handlers;

    public class HandlersManager
    {
        private Dictionary<Type, Handler> handlers;

        public HandlersManager()
        {
            handlers = new Dictionary<Type, Handler>();
        }

        public void AddHandler(Handler handler)
        {
            handlers.Add(handler.GetType(), handler);
        }

        public T GetHandler<T>() where T : Handler
        {
            Type type = typeof(T);
            
            if (IsHandlerExists(type))
                return handlers[type] as T;
            throw new Exception($"The handler of type {type} does not found.");

            bool IsHandlerExists(Type type) => handlers.ContainsKey(type);
        }
    }
}