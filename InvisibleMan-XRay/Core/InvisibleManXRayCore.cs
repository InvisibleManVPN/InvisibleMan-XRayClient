using System;

namespace InvisibleManXRay.Core
{
    using Models;
    using Handlers.Proxies;
    using Values;
    using Utilities;

    public class InvisibleManXRayCore
    {
        private const string LOCAL_HOST = "127.0.0.1";
        private const int DEFAULT_PORT = 10801;
        private const int TEST_PORT = 10802;

        private Func<Config> getConfig;
        private Func<IProxy> getProxy;
        private Action<string> onFailLoadingConfig;

        public void Setup(
            Func<Config> getConfig, 
            Func<IProxy> getProxy, 
            Action<string> onFailLoadingConfig)
        {
            this.getConfig = getConfig;
            this.getProxy = getProxy;
            this.onFailLoadingConfig = onFailLoadingConfig;
        }
        
        public Status LoadConfig()
        {
            Config config = getConfig.Invoke();

            if (config == null)
                return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);

            return LoadConfig(config.Path);
        }

        public Status LoadConfig(string path)
        {
            if (!XRayCoreWrapper.IsFileExists(path))
            {
                onFailLoadingConfig.Invoke(path);
                return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);
            }

            string format = XRayCoreWrapper.GetConfigFormat(path);
            string file = XRayCoreWrapper.LoadConfig(format, path);

            if (!JsonUtility.IsJsonValid(file))
                return new Status(Code.ERROR, SubCode.INVALID_CONFIG, Message.INVALID_CONFIG);

            return new Status(Code.SUCCESS, SubCode.SUCCESS, file);
        }

        public void EnableProxy()
        {
            IProxy proxy = getProxy.Invoke();
            proxy.Enable(LOCAL_HOST, DEFAULT_PORT);
        }

        public void DisableProxy()
        {
            IProxy proxy = getProxy.Invoke();
            proxy.Disable();
        }

        public void Run(string config)
        {
            XRayCoreWrapper.StartServer(config, DEFAULT_PORT);
        }

        public void Stop()
        {
            XRayCoreWrapper.StopServer();
        }

        public bool Test(string config)
        {
            return XRayCoreWrapper.TestConnection(config, TEST_PORT);
        }
    }
}