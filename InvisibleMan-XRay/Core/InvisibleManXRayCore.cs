using System;
using System.Text.Json;
using System.Collections.Generic;

namespace InvisibleManXRay.Core
{
    using Models;
    using Values;

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

            if (!IsValidateJsonConfig())
                yield return new Status(Code.ERROR, Message.INVALID_CONFIG);
            
            yield return new Status(Code.SUCCESS, null);
            XRayCoreWrapper.StartServer(file);

            bool IsValidateJsonConfig()
            {
                try
                {
                    return JsonDocument.Parse(file) != null;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}