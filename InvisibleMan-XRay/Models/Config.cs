namespace InvisibleManXRay.Models
{
    public class Config
    {
        private int index;
        private string path;

        public int Index => index;
        public string Path => path;

        public Config(int index, string path)
        {
            this.index = index;
            this.path = path;
        }
    }
}