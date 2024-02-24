using System.IO;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Data;
    using Values;
    using Utilities;

    public class SettingsHandler : IHandler
    {
        private SettingsData settingsData;

        public SettingsHandler()
        {
            this.settingsData = TryLoadSettingsData();
        }

        public SettingsData GetSettingsData() => settingsData;

        private SettingsData TryLoadSettingsData()
        {
            if (!IsFileExists())
                return new SettingsData();
            
            string rawSettings = File.ReadAllText(Path.SETTINGS);

            if (!IsJsonValid())
                return new SettingsData();

            return JsonConvert.DeserializeObject<SettingsData>(rawSettings);

            bool IsFileExists() => File.Exists(Path.SETTINGS);

            bool IsJsonValid() => JsonUtility.IsJsonValid(rawSettings);
        }
    }
}