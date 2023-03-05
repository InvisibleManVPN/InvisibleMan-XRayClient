using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models
{
    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public int CurrentConfigIndex;

        public UserSettings()
        {
            this.CurrentConfigIndex = 0;
        }

        public int GetCurrentConfigIndex() => CurrentConfigIndex;
    }
}