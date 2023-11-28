namespace InvisibleManXRay.Models.Databases
{
    public class ForeignKey
    {
        private string keyName;
        private string firstColumn;
        private string secondColumn;
        private string properties;

        public string KeyName => keyName;
        public string FirstColumn => firstColumn;
        public string SecondColumn => secondColumn;
        public string Properties => properties;

        public ForeignKey(string keyName, string firstColumn, string secondColumn, string properties)
        {
            this.keyName = keyName;
            this.firstColumn = firstColumn;
            this.secondColumn = secondColumn;
            this.properties = properties;
        }
    }
}