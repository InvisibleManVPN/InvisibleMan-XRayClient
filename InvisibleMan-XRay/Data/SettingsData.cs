using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Data
{
    using Values;

    public class SettingsData
    {
        [JsonProperty(PropertyName = "theme", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Global.DefaultSettings.THEME)]
        public string Theme { get; private set; }

        [JsonProperty(PropertyName = "language", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Global.DefaultSettings.LANGUAGE)]
        public string Language { get; private set; }

        public SettingsData()
        {
            this.Theme = Global.DefaultSettings.THEME;
            this.Language = Global.DefaultSettings.LANGUAGE;
        }
    }
}