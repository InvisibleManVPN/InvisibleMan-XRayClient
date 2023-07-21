namespace InvisibleManXRay.Models
{
    public enum ConfigType { FILE, URL }

    public class Config
    {
        private string path;
        private string name;
        private string updateTime;
        private ConfigType type;
        private int availability;

        public string Path => path;
        public string Name => name;
        public string UpdateTime => updateTime;
        public ConfigType Type => type;
        public int Availability => availability;

        public Config(string path, string name, ConfigType type, string updateTime)
        {
            this.path = path;
            this.name = name;
            this.type = type;
            this.updateTime = updateTime;
            this.availability = Values.Availability.NOT_CHECKED;
        }
        
        public void SetAvailability(int availability) => this.availability = availability;
    }
}