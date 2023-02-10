using System.IO;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;
    using Utilities;

    public class SettingsHandler : Handler
    {
        private UserSettings userSettings;

        public SettingsHandler()
        {
            this.userSettings = LoadUserSettings();
        }

        public int GetConfigIndex() => userSettings.ConfigIndex;

        private UserSettings LoadUserSettings()
        {
            if (!File.Exists(Path.USER_SETTINGS))
                return new UserSettings();

            string rawSettings = File.ReadAllText(Path.USER_SETTINGS);
            if (!JsonUtility.IsJsonValid(rawSettings))
                return new UserSettings();

            return JsonConvert.DeserializeObject<UserSettings>(rawSettings);
        }
    }
}