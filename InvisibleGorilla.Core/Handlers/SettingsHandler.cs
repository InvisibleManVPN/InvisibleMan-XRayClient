using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Handlers
{
    using Models;
    using Values;
    using Utilities;
    using Settings.Startup;

    public class SettingsHandler : Handler
    {
        private UserSettings userSettings;
        private Func<IStartupSetting> startupFactory;

        public UserSettings UserSettings => userSettings;

        public SettingsHandler(Func<IStartupSetting> startupFactory)
        {
            this.startupFactory = startupFactory;
            this.userSettings = LoadUserSettings();
        }

        public void UpdateUserSettings(UserSettings userSettings)
        {
            this.userSettings.Language = userSettings.Language;
            this.userSettings.Mode = userSettings.Mode;
            this.userSettings.Protocol = userSettings.Protocol;
            this.userSettings.LogLevel = userSettings.LogLevel;
            this.userSettings.IsSystemProxyUse = userSettings.IsSystemProxyUse;
            this.userSettings.IsUdpEnable = userSettings.IsUdpEnable;
            this.userSettings.IsRunningAtStartup = userSettings.IsRunningAtStartup;
            this.userSettings.IsStartHidden = userSettings.IsStartHidden;
            this.userSettings.IsAutoConnect = userSettings.IsAutoConnect;
            this.userSettings.IsSendingAnalytics = userSettings.IsSendingAnalytics;
            this.userSettings.ProxyPort = userSettings.ProxyPort;
            this.userSettings.TunPort = userSettings.TunPort;
            this.userSettings.TestPort = userSettings.TestPort;
            this.userSettings.TunIp = userSettings.TunIp;
            this.userSettings.Dns = userSettings.Dns;
            this.userSettings.LogPath = NormalizePath(userSettings.LogPath, Values.Directory.LOGS);
            this.userSettings.AppRulesMode = userSettings.AppRulesMode;
            this.userSettings.AppRules = CloneAppRules(userSettings.AppRules);
            this.userSettings.AppRuleTemplates = CloneAppRuleTemplates(userSettings.AppRuleTemplates);
            this.userSettings.AppRuleTemplateBindings = CloneAppRuleTemplateBindings(userSettings.AppRuleTemplateBindings);

            UpdateStartupSetting();
            SaveUserSettings();
        }

        public void GenerateClientId()
        {
            userSettings.ClientId = IdentificationUtility.GenerateClientId();
            SaveUserSettings();
        }

        public void UpdateCurrentConfigPath(string path)
        {
            userSettings.CurrentConfigPath = NormalizePath(path, Values.Directory.CONFIGS);
            SaveUserSettings();
        }

        public void UpdateMode(Mode mode)
        {
            userSettings.Mode = mode;
            SaveUserSettings();
        }

        private void UpdateStartupSetting()
        {
            IStartupSetting startupSetting = startupFactory();

            if (userSettings.IsRunningAtStartup)
                startupSetting.EnableRunAtStartup();
            else
                startupSetting.DisableRunAtStartup();
        }

        private UserSettings LoadUserSettings()
        {
            Values.Directory.EnsureWritableDirectories();

            if (!File.Exists(Path.USER_SETTINGS))
                return NormalizePaths(new UserSettings());

            string rawSettings = File.ReadAllText(Path.USER_SETTINGS);
            if (!JsonUtility.IsJsonValid(rawSettings))
                return NormalizePaths(new UserSettings());

            return NormalizePaths(JsonConvert.DeserializeObject<UserSettings>(rawSettings));

            UserSettings NormalizePaths(UserSettings settings)
            {
                settings.CurrentConfigPath = NormalizePath(settings.CurrentConfigPath, Values.Directory.CONFIGS);
                settings.LogPath = NormalizePath(settings.LogPath, Values.Directory.LOGS);
                settings.AppRules ??= new System.Collections.Generic.List<AppRule>();
                settings.AppRules = CloneAppRules(settings.AppRules);
                settings.AppRuleTemplates ??= new System.Collections.Generic.List<AppRuleTemplate>();
                settings.AppRuleTemplateBindings ??= new System.Collections.Generic.List<AppRuleTemplateBinding>();
                settings.AppRuleTemplates = CloneAppRuleTemplates(settings.AppRuleTemplates);
                settings.AppRuleTemplateBindings = CloneAppRuleTemplateBindings(settings.AppRuleTemplateBindings);
                return settings;
            }
        }

        private void SaveUserSettings()
        {
            Values.Directory.EnsureWritableDirectories();
            string rawSettings = JsonConvert.SerializeObject(userSettings);
            File.WriteAllText(Path.USER_SETTINGS, rawSettings);
        }

        private static string NormalizePath(string path, string fallback)
        {
            if (string.IsNullOrWhiteSpace(path))
                return fallback;

            if (!System.IO.Path.IsPathRooted(path))
                return System.IO.Path.GetFullPath(System.IO.Path.Combine(Values.Directory.ROOT, path));

            return path;
        }

        private static System.Collections.Generic.List<AppRule> CloneAppRules(System.Collections.Generic.IEnumerable<AppRule>? appRules)
        {
            if (appRules == null)
                return new System.Collections.Generic.List<AppRule>();

            return appRules
                .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.AppId))
                .Select(rule => rule.Clone())
                .ToList();
        }

        private static System.Collections.Generic.List<AppRuleTemplate> CloneAppRuleTemplates(System.Collections.Generic.IEnumerable<AppRuleTemplate>? templates)
        {
            if (templates == null)
                return new System.Collections.Generic.List<AppRuleTemplate>();

            return templates
                .Where(template => template != null && !string.IsNullOrWhiteSpace(template.Id))
                .Select(template =>
                {
                    AppRuleTemplate clone = template.Clone();
                    clone.AppRules = CloneAppRules(clone.AppRules);
                    clone.Name = clone.Name?.Trim() ?? string.Empty;
                    return clone;
                })
                .ToList();
        }

        private static System.Collections.Generic.List<AppRuleTemplateBinding> CloneAppRuleTemplateBindings(System.Collections.Generic.IEnumerable<AppRuleTemplateBinding>? bindings)
        {
            if (bindings == null)
                return new System.Collections.Generic.List<AppRuleTemplateBinding>();

            return bindings
                .Where(binding => binding != null
                    && !string.IsNullOrWhiteSpace(binding.ConfigPath)
                    && !string.IsNullOrWhiteSpace(binding.TemplateId))
                .Select(binding => new AppRuleTemplateBinding(
                    configPath: NormalizeConfigPath(binding.ConfigPath),
                    templateId: binding.TemplateId.Trim()))
                .ToList();
        }

        private static string NormalizeConfigPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return NormalizePath(path, path.Trim());
        }
    }
}
