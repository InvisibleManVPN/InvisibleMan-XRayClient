namespace InvisibleManXRay.Models
{
    public enum Code { SUCCESS, ERROR }
    public enum SubCode { NO_CONFIG, INVALID_CONFIG, SUCCESS }

    public class Status
    {
        private Code code;
        private SubCode subCode;
        private string content;

        public Code Code => code;
        public SubCode SubCode => subCode;
        public string Content => content;

        public Status(Code code, SubCode subCode, string content)
        {
            this.code = code;
            this.subCode = subCode;
            this.content = content;
        }
    }
}