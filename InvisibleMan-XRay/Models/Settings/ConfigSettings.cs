using System.Collections.Generic;

namespace InvisibleManXRay.Models.Settings
{
    public class ConfigSettings
    {
        public int CurrentConfigIndex;
        public List<Config> Configs;

        public ConfigSettings()
        {
            this.CurrentConfigIndex = 0;
            this.Configs = new List<Config>();
        }
    }
}