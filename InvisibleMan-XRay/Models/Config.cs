namespace InvisibleManXRay.Models
{
    public enum ConfigType { FILE, URL }

    public enum GroupType { GENERAL, SUBSCRIPTION }

    public class Config
    {
        private string path;
        private string name;
        private string updateTime;
        private ConfigType type;
        private GroupType group;
        private int availability;

        public string Path => path;
        public string Name => name;
        public string UpdateTime => updateTime;
        public ConfigType Type => type;
        public GroupType Group => group;
        public int Availability => availability;

        public Config(string path, string name, ConfigType type, GroupType group, string updateTime)
        {
            this.path = path;
            this.name = name;
            this.type = type;
            this.group = group;
            this.updateTime = updateTime;
            this.availability = Values.Availability.NOT_CHECKED;
        }
        
        public void SetAvailability(int availability) => this.availability = availability;
    }
}