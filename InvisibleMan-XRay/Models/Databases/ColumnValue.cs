namespace InvisibleManXRay.Models.Databases
{
    public class ColumnValue
    {
        private string column;
        private string value;

        public string Column => column;
        public string Value => value;

        public ColumnValue(string column, string value)
        {
            this.column = column;
            this.value = value;
        }
    }
}