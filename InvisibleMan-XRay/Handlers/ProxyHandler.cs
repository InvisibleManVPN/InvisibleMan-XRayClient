namespace InvisibleManXRay.Handlers
{
    using Models.Proxies;

    public class ProxyHandler : Handler
    {
        private IProxy proxy;

        public ProxyHandler()
        {
            this.proxy = LoadProxy();
        }

        public IProxy GetProxy() => proxy;

        private IProxy LoadProxy()
        {
            #if WINDOWS
                WindowsProxy windowsProxy = new WindowsProxy();
                return windowsProxy;
            #endif
        }
    }
}