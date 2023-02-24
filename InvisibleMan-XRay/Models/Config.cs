using System;

namespace InvisibleManXRay.Models
{
    public enum ConfigType { FILE, URL }
    public enum Availability { NOT_CHECKED, AVAILABLE, TIMEOUT }

    public class Config
    {
        private string path;
        private string name;
        private string updateTime;
        private ConfigType type;
        private Availability availability;

        public string Path => path;
        public string Name => name;
        public string UpdateTime => updateTime;
        public ConfigType Type => type;
        public Availability Availability => availability;

        public Config(string path, string name, ConfigType type, string updateTime)
        {
            this.path = path;
            this.name = name;
            this.type = type;
            this.updateTime = updateTime;
            this.availability = Availability.NOT_CHECKED;
        }

        public void SetAvailability(Availability availability) => this.availability = availability;
    }
}