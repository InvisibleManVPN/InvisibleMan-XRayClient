using System;

namespace InvisibleManXRay.Models
{
    public enum ConfigType { FILE, URL }

    public class Config
    {
        private string path;
        private string name;
        private string updateTime;
        private ConfigType type;

        public string Path => path;
        public string Name => name;
        public string UpdateTime => updateTime;
        public ConfigType Type => type;

        public Config(string path, string name, ConfigType type)
        {
            this.path = path;
            this.name = name;
            this.type = type;
            this.updateTime = DateTime.Now.ToShortDateString();
        }
    }
}