using System;

namespace InvisibleGorillaXRay.Handlers
{
    using Proxies;

    public class ProxyHandler : Handler
    {
        private IProxy proxy;

        public ProxyHandler(Func<IProxy> proxyFactory)
        {
            this.proxy = proxyFactory();
        }

        public IProxy GetProxy() => proxy;
    }
}
