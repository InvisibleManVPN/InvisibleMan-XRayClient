using System;

namespace InvisibleManXRay.Core
{
    using Models;
    using Values;
    using Utilities;

    public class InvisibleManXRayCore
    {
        private const int DEFAULT_PORT = 10801;
        private const int TEST_PORT = 10802;

        private Func<Config> getConfig;
        private Action<string> onFailLoadingConfig;

        public void Setup(Func<Config> getConfig, Action<string> onFailLoadingConfig)
        {
            this.getConfig = getConfig;
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

        public void Run(string config)
        {
            XRayCoreWrapper.StartServer(config, DEFAULT_PORT);
        }

        public bool Test(string config)
        {
            return XRayCoreWrapper.TestConnection(config, TEST_PORT);
        }
    }
}