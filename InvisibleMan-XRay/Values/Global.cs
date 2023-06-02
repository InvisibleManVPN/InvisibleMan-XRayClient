namespace InvisibleManXRay.Values
{
    public static class Global
    {
        public static class StreamNetwork
        {
            public const string WS = "ws";
            public const string H2 = "h2";
            public const string QUIC = "quic";
            public const string GRPC = "grpc";
        }

        public static class StreamSecurity
        {
            public const string TLS = "tls";
            public const string XTLS = "xtls";
        }

        public const string LOCAL_HOST = "127.0.0.1";
        public const string DEFAULT_EMAIL = "t@t.tt";
        public const string DEFAULT_LOG_LEVEL = "warning";
        public const string DEFAULT_SECURITY = "auto";
    }
}