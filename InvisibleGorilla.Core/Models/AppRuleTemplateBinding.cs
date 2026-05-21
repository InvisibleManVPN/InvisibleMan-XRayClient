using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models
{
    public class AppRuleTemplateBinding
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string ConfigPath;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string TemplateId;

        public AppRuleTemplateBinding()
        {
            ConfigPath = "";
            TemplateId = "";
        }

        public AppRuleTemplateBinding(string configPath, string templateId)
        {
            ConfigPath = configPath ?? "";
            TemplateId = templateId ?? "";
        }

        public AppRuleTemplateBinding Clone() => new(ConfigPath, TemplateId);
    }
}
