using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

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
            LoadConfigFiles();
        }

        public void Setup(Func<int> getCurrentConfigIndex)
        {
            this.getCurrentConfigIndex = getCurrentConfigIndex;
        }

        public void LoadConfigFiles()
        {
            configs.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.CONFIGS);
            if (!directoryInfo.Exists)
                return;

            FileInfo[] files = directoryInfo.GetFiles().OrderBy(file => file.CreationTime).ToArray();
            foreach(FileInfo file in files)
            {
                AddConfigToList(CreateConfig(file.FullName));
            }
        }

        public void CopyConfig(string path)
        {
            string destinationPath = $"{Directory.CONFIGS}/{GetFileName(path)}";
            CopyToConfigsDirectory();
            SetFileTime(destinationPath);
            AddConfigToList(CreateConfig(destinationPath));

            void CopyToConfigsDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);          
                File.Copy(path, destinationPath, true);
            }
        }

        public void CreateConfig(string remark, string data, string directory)
        {
            string destinationPath;

            HandleDestinationDirectory();
            SaveToConfigsDirectory();
            SetFileTime(destinationPath);
            AddConfigToList(CreateConfig(destinationPath));

            void HandleDestinationDirectory()
            {
                destinationPath = $"{Directory.CONFIGS}/{remark}.json";
                if (IsDirectoryArgumentExists())
                    destinationPath = $"{directory}/{remark}.json";
            }

            bool IsDirectoryArgumentExists() => !string.IsNullOrEmpty(directory);

            void SaveToConfigsDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);
                File.WriteAllText(destinationPath, data);
            }
        }

        public void CreateConfig(string remark, string data)
        {
            CreateConfig(remark, data, null);
        }

        public void CreateSubscription(string remark, string data)
        {
            string destinationDirectory = $"{Directory.CONFIGS}/{remark}";
            List<string[]> configs = JsonConvert.DeserializeObject<List<string[]>>(data);

            if (!IsAnyConfigExists())
                return;

            CreateSubscriptionDirectory();
            foreach(string[] config in configs)
            {
                CreateConfig(
                    remark: GetConfigRemark(config), 
                    data: GetConfigData(config), 
                    directory: destinationDirectory
                );
            }

            bool IsAnyConfigExists() => configs.Count > 0;

            void CreateSubscriptionDirectory()
            {
                System.IO.Directory.CreateDirectory(destinationDirectory);
            }

            string GetConfigRemark(string[] config) => config[0];

            string GetConfigData(string[] config) => config[1];
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
                type: ConfigType.FILE,
                updateTime: GetFileUpdateTime(path)
            );
        }

        private void SetFileTime(string path)
        {
            File.SetCreationTime(path, DateTime.Now);
            File.SetLastWriteTime(path, DateTime.Now);
        }

        private string GetFileName(string path) => System.IO.Path.GetFileName(path);

        private string GetFileUpdateTime(string path) => System.IO.File.GetLastWriteTime(path).ToShortDateString();
    }
}