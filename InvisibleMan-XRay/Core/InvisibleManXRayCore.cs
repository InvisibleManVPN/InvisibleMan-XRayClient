using System;

namespace InvisibleManXRay.Core
{
    using Models;
    using Values;
    using Utilities;

    public class InvisibleManXRayCore
    {
        private Func<Config> getConfig;

        public void Setup(Func<Config> getConfig)
        {
            this.getConfig = getConfig;
        }

        public Status LoadConfig()
        {
            Config config = getConfig.Invoke();

            if (!XRayCoreWrapper.IsFileExists(config.Path))
                return new Status(Code.ERROR, SubCode.NO_CONFIG, Message.NO_CONFIGS_FOUND);

            string format = XRayCoreWrapper.GetConfigFormat(config.Path);
            string file = XRayCoreWrapper.LoadConfig(format, config.Path);

            if (!JsonUtility.IsJsonValid(file))
                return new Status(Code.ERROR, SubCode.INVALID_CONFIG, Message.INVALID_CONFIG);

            return new Status(Code.SUCCESS, SubCode.SUCCESS, file);
        }

        public void Run(string config)
        {
            XRayCoreWrapper.StartServer(config);
        }
    }
}