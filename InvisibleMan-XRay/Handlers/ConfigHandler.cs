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
        private Action<Config> onAddToConfigSettings;

        public void Setup(
            Func<ConfigSettings> getConfigSettings,
            Action<Config> onAddToConfigSettings)
        {
            this.getConfigSettings = getConfigSettings;
            this.onAddToConfigSettings = onAddToConfigSettings;
        }

        public void AddConfig(string path)
        {
            string configName = GenerateConfigName();
            CopyToConfigsDirectory();
            onAddToConfigSettings.Invoke(CreateConfig());

            string GenerateConfigName() => $"{DateTime.UtcNow.ToFileTimeUtc()}{GetFileExtension()}";
            
            string GetFileExtension() => System.IO.Path.GetExtension(path);

            string GetFileName() => System.IO.Path.GetFileName(path);

            void CopyToConfigsDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);          
                File.Copy(path, $"{Directory.CONFIGS}/{configName}");
            }

            Config CreateConfig()
            {
                return new Config(
                    path: $"{Directory.CONFIGS}/{configName}",
                    name: GetFileName(),
                    type: ConfigType.FILE
                );
            }
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