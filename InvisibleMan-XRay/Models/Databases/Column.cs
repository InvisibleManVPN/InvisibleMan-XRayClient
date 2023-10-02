namespace InvisibleManXRay.Models.Databases
{
    public class Column
    {
        private string name;
        private string type;
        private string properties;

        public string Name => name;
        public string Type => type;
        public string Properties => properties;

        public Column(string name, string type, string properties = "")
        {
            this.name = name;
            this.type = type;
            this.properties = properties;
        }
    }
}