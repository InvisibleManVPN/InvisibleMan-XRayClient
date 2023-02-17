using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class ConfigHandler : Handler
    {
        private Dictionary<string, Config> configs;
        private Func<int> getCurrentConfigIndex;

        public ConfigHandler()
        {
            this.configs = new Dictionary<string, Config>();
            LoadConfigsFile();

            void LoadConfigsFile()
            {
                string[] files = System.IO.Directory.GetFiles(Directory.CONFIGS);

                foreach(string file in files)
                {
                    AddConfigToList(CreateConfig(file));
                }
            }
        }

        public void Setup(Func<int> getCurrentConfigIndex)
        {
            this.getCurrentConfigIndex = getCurrentConfigIndex;
        }

        public void AddConfig(string path)
        {
            CopyToConfigsDirectory();
            AddConfigToList(CreateConfig(path));

            void CopyToConfigsDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);          
                File.Copy(path, $"{Directory.CONFIGS}/{GetFileName(path)}", true);
            }
        }

        public Config GetCurrentConfig()
        {
            int currentConfigIndex = getCurrentConfigIndex.Invoke();
            
            try
            {
                return configs.ElementAt(currentConfigIndex).Value;
            }
            catch
            {
                return null;
            }
        }

        public List<Config> GetAllConfigs() => configs.Select(config => config.Value).ToList();

        public void RemoveConfigFromList(string path)
        {
            if (configs.ContainsKey(path))
                configs.Remove(path);
        }

        private void AddConfigToList(Config config)
        {
            if (configs.ContainsKey(config.Path))
                configs[config.Path] = config;
            else
                configs.Add(config.Path, config);
        }

        private Config CreateConfig(string path)
        {
            return new Config(
                path: $"{Directory.CONFIGS}/{GetFileName(path)}",
                name: GetFileName(path),
                type: ConfigType.FILE
            );
        }

        private string GetFileName(string path) => System.IO.Path.GetFileName(path);
    }
}