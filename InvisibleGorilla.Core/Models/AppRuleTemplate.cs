using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models
{
    public class AppRuleTemplate
    {
        public const string DefaultTemplateId = "__default__";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string Id;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string Name;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(AppRulesMode.ALL_APPS)]
        public AppRulesMode Mode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<AppRule> AppRules;

        public AppRuleTemplate()
        {
            Id = "";
            Name = "";
            Mode = AppRulesMode.ALL_APPS;
            AppRules = new List<AppRule>();
        }

        public AppRuleTemplate(string id, string name, AppRulesMode mode, List<AppRule>? appRules = null)
        {
            Id = id ?? "";
            Name = name ?? "";
            Mode = mode;
            AppRules = appRules ?? new List<AppRule>();
        }

        public AppRuleTemplate Clone()
        {
            return new AppRuleTemplate(
                id: Id,
                name: Name,
                mode: Mode,
                appRules: (AppRules ?? new List<AppRule>())
                    .FindAll(rule => rule != null)
                    .ConvertAll(rule => rule.Clone()));
        }
    }
}
