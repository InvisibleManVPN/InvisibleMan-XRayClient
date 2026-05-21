using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models
{
    public enum AppRulesMode
    {
        ALL_APPS = 0,
        DISABLED = ALL_APPS,
        BYPASS_SELECTED_APPS = 1,
        ONLY_SELECTED_APPS = 2
    }

    public class AppRule
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string AppId;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string DisplayName;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string IconRef;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(true)]
        public bool Enabled;

        public AppRule()
        {
            AppId = "";
            DisplayName = "";
            IconRef = "";
            Enabled = true;
        }

        public AppRule(string appId, string displayName, string iconRef, bool enabled = true)
        {
            AppId = appId ?? "";
            DisplayName = displayName ?? "";
            IconRef = iconRef ?? "";
            Enabled = enabled;
        }

        public AppRule Clone() => new(AppId, DisplayName, IconRef, Enabled);
    }
}
