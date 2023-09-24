using System.Linq;
using System.Collections.Generic;

namespace InvisibleManXRay.Handlers.Configs
{
    using Models;
    using Utilities;

    public abstract class BaseConfig
    {
        protected Dictionary<string, Config> configs;

        public BaseConfig()
        {
            this.configs = new Dictionary<string, Config>();
        }

        public List<Config> GetAllConfigs()
        {
            return configs.Select(config => config.Value).ToList();
        }

        public void AddConfigToList(Config config)
        {
            if (configs.ContainsKey(config.Path))
                configs[config.Path] = config;
            else
                configs.Add(config.Path, config);
        }

        public void RemoveConfigFromList(string path)
        {
            if (configs.ContainsKey(path))
                configs.Remove(path);
        }

        protected void SetFileTime(string path) => FileUtility.SetTimeToNow(path);

        protected string GetFileName(string path) => FileUtility.GetFileName(path);

        protected string GetDirectory(string path) => FileUtility.GetDirectory(path);

        protected string GetFileUpdateTime(string path) => FileUtility.GetFileUpdateTime(path);

        public abstract void LoadFiles(string path);

        public abstract Config CreateConfigModel(string path);
    }
}