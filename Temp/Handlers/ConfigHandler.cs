using System;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers
{
    using Configs;
    using Models;
    using Values;
    using Utilities;

    public class ConfigHandler : Handler
    {
        private GeneralConfig generalConfig;
        private SubscriptionConfig subscriptionConfig;

        private Func<string> getCurrentConfigPath;

        public ConfigHandler()
        {
            this.generalConfig = new GeneralConfig();
            this.subscriptionConfig = new SubscriptionConfig();
        }

        public void Setup(Func<string> getCurrentConfigPath)
        {
            this.getCurrentConfigPath = getCurrentConfigPath;
            subscriptionConfig.Setup(getCurrentConfigPath);
        }

        public void LoadFiles(GroupType group, string path)
        {
            BaseConfig config = group == GroupType.GENERAL ? generalConfig : subscriptionConfig;
            config.LoadFiles(path);
        }

        public void CreateConfig(string remark, string data) => generalConfig.CreateConfig(remark, data);

        public void CreateSubscription(string remark, string url, string data) => subscriptionConfig.CreateSubscription(remark, url, data);

        public void DeleteSubscription(Subscription subscription) => subscriptionConfig.DeleteSubscription(subscription);

        public void CopyConfig(string path) => generalConfig.CopyConfig(path);

        public Config GetCurrentConfig() => CreateConfigModel(getCurrentConfigPath.Invoke());

        public void RemoveConfigFromList(string path) => GetCurrentBaseConfig().RemoveConfigFromList(path);

        public List<Config> GetAllGeneralConfigs() 
        {
            generalConfig.LoadFiles();
            return generalConfig.GetAllConfigs();
        }

        public List<Config> GetAllSubscriptionConfigs(string path) 
        {
            subscriptionConfig.LoadFiles(path);
            return subscriptionConfig.GetAllConfigs();
        }

        public List<Subscription> GetAllGroups()
        {
            subscriptionConfig.LoadGroups();
            return subscriptionConfig.GetAllGroups();
        }

        public Config CreateConfigModel(string path)
        {
            if (string.IsNullOrEmpty(path) || !FileUtility.IsFileExists(path))
                return null;

            return GetCurrentBaseConfig().CreateConfigModel(path);
        }

        public bool IsCurrentPathEqualRootConfigPath()
        {
            return GetCurrentConfigDirectory() == GetRootConfigDirectory();

            string GetCurrentConfigDirectory()
            {
                string path = getCurrentConfigPath.Invoke();
                string directory = FileUtility.GetDirectory(path);

                if (string.IsNullOrEmpty(path) || !FileUtility.IsFileExists(path))
                    directory = Directory.CONFIGS;
                
                return FileUtility.GetFullPath(directory);
            }

            string GetRootConfigDirectory()
            {
                return FileUtility.GetFullPath(Directory.CONFIGS);
            }
        }

        private BaseConfig GetCurrentBaseConfig()
        {
            if (IsCurrentPathEqualRootConfigPath())
                return generalConfig;
            else
                return subscriptionConfig;
        }
    }
}