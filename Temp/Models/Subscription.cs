using System.IO;

namespace InvisibleManXRay.Models
{
    public class Subscription
    {
        private string url;
        private DirectoryInfo directory;

        public string Url => url;
        public DirectoryInfo Directory => directory;

        public Subscription(string url, DirectoryInfo directory)
        {
            this.url = url;
            this.directory = directory;
        }
    }
}