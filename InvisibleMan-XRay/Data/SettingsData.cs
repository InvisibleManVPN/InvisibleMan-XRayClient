using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Data
{
    using Values;

    public class SettingsData
    {
        [JsonProperty(PropertyName = "language", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Global.DefaultSettings.LANGUAGE)]
        public string Language { get; private set; }

        public SettingsData()
        {
            this.Language = Global.DefaultSettings.LANGUAGE;
        }
    }
}