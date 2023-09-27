using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers.Configs
{
    using Models;
    using Values;
    using Utilities;

    public class SubscriptionConfig : BaseConfig
    {
        private List<Subscription> groups;
        private Func<Config> createConfigModel;

        public SubscriptionConfig() : base()
        {
            this.groups = new List<Subscription>();
        }

        public void Setup(Func<string> getCurrentConfigPath)
        {
            LoadFiles(getCurrentConfigPath.Invoke());
        }
        
        public void LoadGroups()
        {
            groups.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.CONFIGS);
            if (!directoryInfo.Exists)
                return;
            
            DirectoryInfo[] directories = directoryInfo.GetDirectories().Where(
                directory => directory.GetFiles().Any(file => file.Name == "info.dat") &&
                    directory.GetFiles().Count() > 1
            ).OrderBy(
                directory => directory.CreationTime
            ).ToArray();

            foreach(DirectoryInfo directory in directories)
            {
                AddDirectoryToList(directory);
            }
        }

        public List<Subscription> GetAllGroups()
        {
            return groups;
        }

        public void CreateSubscription(string remark, string url, string data)
        {
            string destinationDirectory = $"{Directory.CONFIGS}/{remark}";
            List<string[]> configs = JsonConvert.DeserializeObject<List<string[]>>(data);

            if (!IsAnyConfigExists())
                return;

            CreateInfoFile();
            foreach(string[] config in configs)
            {
                CreateConfigFile(
                    remark: GetConfigRemark(config), 
                    data: GetConfigData(config)
                );
            }

            bool IsAnyConfigExists() => configs.Count > 0;

            void CreateInfoFile()
            {
                string destinationPath = $"{destinationDirectory}/info.dat";
                SaveToDirectory(destinationPath, url);
            }

            void CreateConfigFile(string remark, string data)
            {
                string destinationPath = $"{destinationDirectory}/{remark}.json";
                SaveToDirectory(destinationPath, data);
                SetFileTime(destinationPath);
                AddConfigToList(CreateConfigModel(destinationPath));

                void SetFileTime(string path) => FileUtility.SetFileTimeToNow(path);
            }

            void SaveToDirectory(string destinationPath, string data)
            {
                FileUtility.TryDeleteDirectory(destinationDirectory);
                FileUtility.CreateDirectory(destinationDirectory);
                FileUtility.SetDirectoryTimeToNow(destinationDirectory);
                File.WriteAllText(destinationPath, data);
            }

            string GetConfigRemark(string[] config) => config[0];

            string GetConfigData(string[] config) => config[1];
        }

        public void DeleteSubscription(Subscription subscription)
        {
            System.IO.Directory.Delete(subscription.Directory.FullName, true);
            groups.Remove(subscription);
        }

        public override void LoadFiles(string path)
        {
            LoadGroups();
            configs.Clear();

            if(!IsAnyGroupExists())
                return;

            DirectoryInfo directoryInfo = new DirectoryInfo(GetConfigDirectory());

            if (!IsValidDirectory())
                directoryInfo = groups.Last().Directory;

            FileInfo[] files = directoryInfo.GetFiles().Where(
                file => file.Extension != ".dat"
            ).OrderBy(file => file.CreationTime).ToArray();

            foreach(FileInfo file in files)
            {
                AddConfigToList(CreateConfigModel(file.FullName));
            }

            bool IsAnyGroupExists() => groups.Count > 0;

            bool IsValidDirectory() => directoryInfo.Exists && GetConfigDirectory() != GetRootConfigDirectory();

            string GetConfigDirectory()
            {
                string directory = path;
                if (!FileUtility.IsDirectory(path))
                    directory = FileUtility.GetDirectory(path);

                path = $"{directory}/info.dat";
                if (!FileUtility.IsFileExists(path))
                    directory = Directory.CONFIGS;
                
                return FileUtility.GetFullPath(directory);
            }

            string GetRootConfigDirectory()
            {
                return FileUtility.GetFullPath(Directory.CONFIGS);
            }
        }

        public override Config CreateConfigModel(string path)
        {
            return new Config(
                path: $"{GetDirectory(path)}/{GetFileName(path)}",
                name: GetFileName(path),
                type: ConfigType.FILE,
                group: GroupType.SUBSCRIPTION,
                updateTime: GetFileUpdateTime(path)
            );
        }

        private void AddDirectoryToList(DirectoryInfo directory)
        {
            groups.Add(new Subscription(FetchUrlFromDirectory(), directory));

            string FetchUrlFromDirectory() => File.ReadAllText($"{directory.FullName}/info.dat");
        }
    }
}