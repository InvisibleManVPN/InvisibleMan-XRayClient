namespace InvisibleManXRay.Models
{
    public enum Code { SUCCESS, ERROR, INFO }
    public enum SubCode { 
        NO_CONFIG, INVALID_CONFIG, SUCCESS, UNSUPPORTED_LINK, CANT_CONNECT, 
        UPDATE_AVAILABLE, UPDATE_UNAVAILABLE, BROADCAST_UNAVAILABLE, CANT_CONNECT_TO_TUNNEL_SERVICE,
        CANT_PROXY, CANT_TUNNEL, CANCELED
    }

    public class Status
    {
        private Code code;
        private SubCode subCode;
        private object content;

        public Code Code => code;
        public SubCode SubCode => subCode;
        public object Content => content;

        public Status(Code code, SubCode subCode, object content)
        {
            this.code = code;
            this.subCode = subCode;
            this.content = content;
        }
    }
}