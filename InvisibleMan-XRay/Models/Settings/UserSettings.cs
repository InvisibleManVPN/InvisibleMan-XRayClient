using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Settings
{
    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(null)]
        public ConfigSettings ConfigSettings;

        public UserSettings()
        {
            this.ConfigSettings = null;
        }
    }
}