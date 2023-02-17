using System;
using System.IO;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Models.Settings;
    using Values;

    public class ConfigHandler : Handler
    {
        private Func<ConfigSettings> getConfigSettings;

        public void Setup(Func<ConfigSettings> getConfigSettings)
        {
            this.getConfigSettings = getConfigSettings;
        }

        public void AddConfig(string path)
        {
            string fileName = System.IO.Path.GetFileName(path);
            
            System.IO.Directory.CreateDirectory(Directory.CONFIGS);                
            File.Copy(path, $"{Directory.CONFIGS}/{fileName}");
        }

        public Config GetCurrentConfig()
        {
            ConfigSettings configSettings = getConfigSettings.Invoke();
            
            try
            {
                int currentConfigIndex = configSettings.CurrentConfigIndex;
                return configSettings.Configs[currentConfigIndex];
            }
            catch
            {
                return null;
            }
        }
    }
}