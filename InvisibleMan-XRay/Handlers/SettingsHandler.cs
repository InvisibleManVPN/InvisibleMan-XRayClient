using System.IO;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;
    using Utilities;
    using Settings.Startup;

    public class SettingsHandler : Handler
    {
        private UserSettings userSettings;

        public UserSettings UserSettings => userSettings;

        public SettingsHandler()
        {
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
            this.userSettings.IsSendingAnalytics = userSettings.IsSendingAnalytics;
            this.userSettings.ProxyPort = userSettings.ProxyPort;
            this.userSettings.TunPort = userSettings.TunPort;
            this.userSettings.TestPort = userSettings.TestPort;
            this.userSettings.TunIp = userSettings.TunIp;
            this.userSettings.Dns = userSettings.Dns;
            this.userSettings.LogPath = userSettings.LogPath;

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
            userSettings.CurrentConfigPath = string.IsNullOrEmpty(path) ? Directory.CONFIGS : path;
            SaveUserSettings();
        }

        public void UpdateMode(Mode mode)
        {
            userSettings.Mode = mode;
            SaveUserSettings();
        }

        private void UpdateStartupSetting()
        {
            IStartupSetting startupSetting = new WindowsStartupSetting();

            if (userSettings.IsRunningAtStartup)
                startupSetting.EnableRunAtStartup();
            else
                startupSetting.DisableRunAtStartup();
        }

        private UserSettings LoadUserSettings()
        {
            if (!File.Exists(Path.USER_SETTINGS))
                return new UserSettings();

            string rawSettings = File.ReadAllText(Path.USER_SETTINGS);
            if (!JsonUtility.IsJsonValid(rawSettings))
                return new UserSettings();

            return JsonConvert.DeserializeObject<UserSettings>(rawSettings);
        }

        private void SaveUserSettings()
        {
            string rawSettings = JsonConvert.SerializeObject(userSettings);
            File.WriteAllText(Path.USER_SETTINGS, rawSettings);
        }
    }
}