using System;
using System.Collections.Generic;

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

        public IEnumerable<Status> Run()
        {
            Config config = getConfig.Invoke();

            if (!XRayCoreWrapper.IsFileExists(config.Path))
                yield return new Status(Code.ERROR, Message.NO_CONFIGS_FOUND);

            string format = XRayCoreWrapper.GetConfigFormat(config.Path);
            string file = XRayCoreWrapper.LoadConfig(format, config.Path);

            if (!JsonUtility.IsJsonValid(file))
                yield return new Status(Code.ERROR, Message.INVALID_CONFIG);
            
            yield return new Status(Code.SUCCESS, null);
            XRayCoreWrapper.StartServer(file);
        }
    }
}