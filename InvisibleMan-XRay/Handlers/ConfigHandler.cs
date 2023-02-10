using System;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class ConfigHandler : Handler
    {
        private Func<int> getConfigIndex;

        public void Setup(Func<int> getConfigIndex)
        {
            this.getConfigIndex = getConfigIndex;
        }

        public Config GetConfig() => new Config(
            index: getConfigIndex.Invoke(),
            path: $"{Directory.CONFIGS}/config-{getConfigIndex.Invoke()}"
        );
    }
}