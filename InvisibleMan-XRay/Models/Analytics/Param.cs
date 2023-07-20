namespace InvisibleManXRay.Models.Analytics
{
    public class Param
    {
        public string Key;
        public object  Value;

        public Param(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}