namespace InvisibleManXRay.Models
{
    public enum Code { SUCCESS, ERROR }

    public class Status
    {
        private Code code;
        private string content;

        public Code Code => code;
        public string Content => content;

        public Status(Code code, string content)
        {
            this.code = code;
            this.content = content;
        }
    }
}