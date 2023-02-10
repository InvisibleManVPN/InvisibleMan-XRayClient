using System.IO;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class SettingsHandler : Handler
    {
        private UserSettings userSettings;

        public int GetConfigIndex() => userSettings.ConfigIndex;

        public SettingsHandler()
        {
            this.userSettings = TryLoadUserSettingsFromFile();
        }

        private UserSettings TryLoadUserSettingsFromFile()
        {
            if (!File.Exists(Path.USER_SETTINGS))
                return new UserSettings();

            string rawSettings = File.ReadAllText(Path.USER_SETTINGS);
            return JsonConvert.DeserializeObject<UserSettings>(rawSettings);
        }
    }
}