using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models
{
    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(-1)]
        public int ConfigIndex;

        public UserSettings()
        {
            this.ConfigIndex = -1;
        }
    }
}