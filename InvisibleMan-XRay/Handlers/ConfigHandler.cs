using System;

namespace InvisibleManXRay.Handlers
{
    using Models;

    public class ConfigHandler : Handler
    {
        private Config config;
        private Func<int> getConfigIndex;

        public void Setup(Func<int> getConfigIndex)
        {
            this.getConfigIndex = getConfigIndex;
        }
    }
}