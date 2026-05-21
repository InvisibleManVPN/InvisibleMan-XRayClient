using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models
{
    public enum Mode { PROXY, TUN }

    public enum Protocol { HTTP, SOCKS }

    public enum LogLevel { NONE, DEBUG, INFO, WARNING, ERROR }

    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("")]
        public string ClientId;

        [JsonProperty(PropertyName = "language", DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("en-US")]
        public string Language;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("./Configs")]
        public string CurrentConfigPath;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Mode.PROXY)]
        public Mode Mode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Protocol.HTTP)]
        public Protocol Protocol;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(true)]
        public bool IsSystemProxyUse;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(true)]
        public bool IsUdpEnable;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(false)]
        public bool IsRunningAtStartup;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(false)]
        public bool IsStartHidden;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(false)]
        public bool IsAutoConnect;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(true)]
        public bool IsSendingAnalytics;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10801)]
        public int ProxyPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10802)]
        public int TunPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10803)]
        public int TestPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("10.0.236.10")]
        public string TunIp;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("8.8.8.8")]
        public string Dns;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(LogLevel.NONE)]
        public LogLevel LogLevel;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("./Logs")]
        public string LogPath;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(AppRulesMode.ALL_APPS)]
        public AppRulesMode AppRulesMode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<AppRule> AppRules;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<AppRuleTemplate> AppRuleTemplates;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<AppRuleTemplateBinding> AppRuleTemplateBindings;

        public UserSettings()
        {
            this.ClientId = "";
            this.Language = "en-US";
            this.CurrentConfigPath = "./Configs";
            this.Mode = Mode.PROXY;
            this.Protocol = Protocol.HTTP;
            this.IsSystemProxyUse = true;
            this.IsUdpEnable = true;
            this.IsRunningAtStartup = false;
            this.IsStartHidden = false;
            this.IsAutoConnect = false;
            this.IsSendingAnalytics = true;
            this.TunIp = "10.0.236.10";
            this.ProxyPort = 10801;
            this.TunPort = 10802;
            this.TestPort = 10803;
            this.Dns = "8.8.8.8";
            this.LogLevel = LogLevel.NONE;
            this.LogPath = "./Logs";
            this.AppRulesMode = AppRulesMode.ALL_APPS;
            this.AppRules = new List<AppRule>();
            this.AppRuleTemplates = new List<AppRuleTemplate>();
            this.AppRuleTemplateBindings = new List<AppRuleTemplateBinding>();
        }

        public UserSettings(
            string language,
            Mode mode,
            Protocol protocol,
            LogLevel logLevel,
            bool isUdpEnable,
            bool isSystemProxyUse,
            bool isRunningAtStartup,
            bool isStartHidden,
            bool isAutoConnect,
            bool isSendingAnalytics,
            int proxyPort,
            int tunPort,
            int testPort,
            string tunIp,
            string dns,
            string logPath,
            AppRulesMode appRulesMode = AppRulesMode.ALL_APPS,
            List<AppRule>? appRules = null,
            List<AppRuleTemplate>? appRuleTemplates = null,
            List<AppRuleTemplateBinding>? appRuleTemplateBindings = null
        )
        {
            this.Language = language;
            this.Mode = mode;
            this.Protocol = protocol;
            this.LogLevel = logLevel;
            this.IsSystemProxyUse = isSystemProxyUse;
            this.IsUdpEnable = isUdpEnable;
            this.IsRunningAtStartup = isRunningAtStartup;
            this.IsStartHidden = isStartHidden;
            this.IsAutoConnect = isAutoConnect;
            this.IsSendingAnalytics = isSendingAnalytics;
            this.ProxyPort = proxyPort;
            this.TunPort = tunPort;
            this.TestPort = testPort;
            this.TunIp = tunIp;
            this.Dns = dns;
            this.LogPath = logPath;
            this.AppRulesMode = NormalizeAppRulesMode(appRulesMode);
            this.AppRules = NormalizeAppRules(appRules);
            this.AppRuleTemplates = NormalizeAppRuleTemplates(appRuleTemplates);
            this.AppRuleTemplateBindings = NormalizeTemplateBindings(appRuleTemplateBindings);
        }

        public string GetClientId() => ClientId;

        public string GetLanguage() => Language;

        public string GetCurrentConfigPath() => CurrentConfigPath;

        public Mode GetMode() => Mode;

        public Protocol GetProtocol() => Protocol;

        public bool GetSystemProxyUsed() => IsSystemProxyUse;

        public bool GetUdpEnabled() => IsUdpEnable;

        public bool GetRunningAtStartupEnabled() => IsRunningAtStartup;

        public bool GetStartHiddenEnabled() => IsStartHidden;

        public bool GetAutoConnectEnabled() => IsAutoConnect;

        public bool GetSendingAnalyticsEnabled() => IsSendingAnalytics;

        public int GetProxyPort() => ProxyPort;

        public int GetTunPort() => TunPort;

        public int GetTestPort() => TestPort;

        public string GetTunIp() => TunIp;

        public string GetDns() => Dns;

        public LogLevel GetLogLevel() => LogLevel;

        public string GetLogPath() => LogPath;

        public AppRulesMode GetAppRulesMode() => NormalizeAppRulesMode(AppRulesMode);

        public List<AppRule> GetAppRules() => NormalizeAppRules(AppRules);

        public List<AppRule> GetEnabledAppRules() => GetAppRules().Where(rule => rule.Enabled).ToList();

        public List<AppRuleTemplate> GetAppRuleTemplates() => NormalizeAppRuleTemplates(AppRuleTemplates);

        public List<AppRuleTemplateBinding> GetAppRuleTemplateBindings() => NormalizeTemplateBindings(AppRuleTemplateBindings);

        public List<AppRuleTemplate> GetAvailableAppRuleTemplates()
        {
            List<AppRuleTemplate> templates = new() { CreateDefaultAppRuleTemplate() };
            templates.AddRange(
                GetAppRuleTemplates()
                    .Where(template => !string.Equals(
                        template.Id,
                        AppRuleTemplate.DefaultTemplateId,
                        StringComparison.OrdinalIgnoreCase))
                    .Select(template => template.Clone()));
            return templates;
        }

        public string GetBoundAppRuleTemplateId(string? configPath = null)
        {
            string normalizedConfigPath = NormalizeConfigPath(configPath ?? CurrentConfigPath);
            if (string.IsNullOrWhiteSpace(normalizedConfigPath))
                return AppRuleTemplate.DefaultTemplateId;

            AppRuleTemplateBinding? binding = GetAppRuleTemplateBindings()
                .LastOrDefault(candidate => string.Equals(
                    candidate.ConfigPath,
                    normalizedConfigPath,
                    StringComparison.OrdinalIgnoreCase));

            return binding?.TemplateId ?? AppRuleTemplate.DefaultTemplateId;
        }

        public AppRuleTemplate GetAppRuleTemplateById(string? templateId)
        {
            string normalizedTemplateId = NormalizeTemplateId(templateId);
            if (string.IsNullOrWhiteSpace(normalizedTemplateId)
                || string.Equals(normalizedTemplateId, AppRuleTemplate.DefaultTemplateId, StringComparison.OrdinalIgnoreCase))
            {
                return CreateDefaultAppRuleTemplate();
            }

            AppRuleTemplate? template = GetAppRuleTemplates()
                .LastOrDefault(candidate => string.Equals(
                    candidate.Id,
                    normalizedTemplateId,
                    StringComparison.OrdinalIgnoreCase));

            return template?.Clone() ?? CreateDefaultAppRuleTemplate();
        }

        public AppRuleTemplate GetEffectiveAppRuleTemplate(string? configPath = null)
        {
            return GetAppRuleTemplateById(GetBoundAppRuleTemplateId(configPath));
        }

        public AppRulesMode GetEffectiveAppRulesMode(string? configPath = null)
        {
            return NormalizeAppRulesMode(GetEffectiveAppRuleTemplate(configPath).Mode);
        }

        public List<AppRule> GetEffectiveAppRules(string? configPath = null)
        {
            return NormalizeAppRules(GetEffectiveAppRuleTemplate(configPath).AppRules);
        }

        public List<AppRule> GetEffectiveEnabledAppRules(string? configPath = null)
        {
            return GetEffectiveAppRules(configPath)
                .Where(rule => rule.Enabled)
                .ToList();
        }

        private static List<AppRule> NormalizeAppRules(List<AppRule>? appRules)
        {
            if (appRules == null)
                return new List<AppRule>();

            return appRules
                .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.AppId))
                .Select(rule => rule.Clone())
                .ToList();
        }

        private static List<AppRuleTemplate> NormalizeAppRuleTemplates(List<AppRuleTemplate>? templates)
        {
            if (templates == null)
                return new List<AppRuleTemplate>();

            return templates
                .Where(template => template != null && !string.IsNullOrWhiteSpace(template.Id))
                .Select(template =>
                {
                    AppRuleTemplate clone = template.Clone();
                    clone.Id = NormalizeTemplateId(clone.Id);
                    clone.Mode = NormalizeAppRulesMode(clone.Mode);
                    clone.AppRules = NormalizeAppRules(clone.AppRules);
                    clone.Name = clone.Name?.Trim() ?? string.Empty;
                    return clone;
                })
                .Where(template => !string.IsNullOrWhiteSpace(template.Id)
                    && !string.Equals(template.Id, AppRuleTemplate.DefaultTemplateId, StringComparison.OrdinalIgnoreCase))
                .GroupBy(template => template.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last().Clone())
                .ToList();
        }

        private static List<AppRuleTemplateBinding> NormalizeTemplateBindings(List<AppRuleTemplateBinding>? bindings)
        {
            if (bindings == null)
                return new List<AppRuleTemplateBinding>();

            return bindings
                .Where(binding => binding != null)
                .Select(binding => new AppRuleTemplateBinding(
                    configPath: NormalizeConfigPath(binding.ConfigPath),
                    templateId: NormalizeTemplateId(binding.TemplateId)))
                .Where(binding => !string.IsNullOrWhiteSpace(binding.ConfigPath)
                    && !string.IsNullOrWhiteSpace(binding.TemplateId))
                .GroupBy(binding => binding.ConfigPath, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last().Clone())
                .ToList();
        }

        private AppRuleTemplate CreateDefaultAppRuleTemplate()
        {
            return new AppRuleTemplate(
                id: AppRuleTemplate.DefaultTemplateId,
                name: string.Empty,
                mode: NormalizeAppRulesMode(AppRulesMode),
                appRules: NormalizeAppRules(AppRules));
        }

        private static string NormalizeTemplateId(string? templateId)
        {
            return string.IsNullOrWhiteSpace(templateId)
                ? string.Empty
                : templateId.Trim();
        }

        private static string NormalizeConfigPath(string? configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                return string.Empty;

            try
            {
                return System.IO.Path.GetFullPath(configPath.Trim());
            }
            catch
            {
                return configPath.Trim();
            }
        }

        private static AppRulesMode NormalizeAppRulesMode(AppRulesMode mode)
        {
            return mode switch
            {
                AppRulesMode.BYPASS_SELECTED_APPS => AppRulesMode.BYPASS_SELECTED_APPS,
                AppRulesMode.ONLY_SELECTED_APPS => AppRulesMode.ONLY_SELECTED_APPS,
                _ => AppRulesMode.ALL_APPS
            };
        }
    }
}