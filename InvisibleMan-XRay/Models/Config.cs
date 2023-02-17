namespace InvisibleManXRay.Models
{
    public enum ConfigType { FILE, URL }

    public class Config
    {
        private int index;
        private string path;
        private string name;
        private ConfigType type;

        public int Index => index;
        public string Path => path;
        public string Name => name;
        public ConfigType Type => type;

        public Config(string path, string name, ConfigType type)
        {
            this.path = path;
            this.name = name;
            this.type = type;
        }
    }
}