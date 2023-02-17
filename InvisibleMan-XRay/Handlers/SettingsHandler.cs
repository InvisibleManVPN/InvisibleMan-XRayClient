using System.IO;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Models.Settings;
    using Values;
    using Utilities;

    public class SettingsHandler : Handler
    {
        private UserSettings userSettings;

        public SettingsHandler()
        {
            this.userSettings = LoadUserSettings();
        }

        public ConfigSettings GetConfigSettings() => userSettings.ConfigSettings;

        public void AddToConfigSettings(Config config)
        {
            InitializeConfigSettings();
            userSettings.ConfigSettings.Configs.Add(config);
            SaveUserSettings();

            void InitializeConfigSettings()
            {
                if (userSettings.ConfigSettings == null)
                    userSettings.ConfigSettings = new ConfigSettings();
            }
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