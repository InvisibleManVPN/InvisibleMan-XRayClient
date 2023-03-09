namespace InvisibleManXRay.Models.Templates
{
    using Utilities;
    using Values;

    public abstract class Template
    {
        private V2Ray v2Ray;
        protected abstract General General { get; }
        protected abstract V2Ray.Outbound.Settings OutboundSettings { get; }

        public V2Ray ConvertToV2Ray()
        {
            v2Ray = new V2Ray() {
                log = Log,
                inbounds = Inbounds,
                outbounds = Outbounds
            };

            return v2Ray;
        }

        private V2Ray.Log Log => new V2Ray.Log() {
            loglevel = Global.DEFAULT_LOG_LEVEL,
            access = "",
            error = ""
        };

        private V2Ray.Inbound[] Inbounds => new V2Ray.Inbound[] {
            new V2Ray.Inbound() {
                port = 10801,
                listen = "127.0.0.1",
                protocol = "http",
                settings = new V2Ray.Inbound.Settings() {
                    udp = true
                }
            }
        };

        private V2Ray.Outbound[] Outbounds => new V2Ray.Outbound[] {
            new V2Ray.Outbound() {
                protocol = General.type,
                settings = OutboundSettings,
                streamSettings = new V2Ray.StreamSettings() {
                    network = General.streamNetwork,
                    security = General.streamSecurity,
                    tlsSettings = TlsSettings,
                    xtlsSettings = XtlsSettings,
                    wsSettings = WsSettings,
                    httpSettings = HttpSettings,
                    quicSettings = QuicSettings,
                    tcpSettings = TcpSettings
                }
            }
        };

        private V2Ray.StreamSettings.TlsSettings TlsSettings
        {
            get
            {
                V2Ray.StreamSettings.TlsSettings tlsSettings = null;

                if (General.streamSecurity == Global.StreamSecurity.TLS)
                {
                    tlsSettings = new V2Ray.StreamSettings.TlsSettings() {
                        allowInsecure = false,
                        alpn = new[] { General.alpn },
                        fingerprint = General.fingerprint
                    };

                    if (!string.IsNullOrWhiteSpace(General.sni))
                        tlsSettings.serverName = General.sni;
                    else if (!string.IsNullOrWhiteSpace(General.requestHost))
                        tlsSettings.serverName = General.requestHost;
                }

                return tlsSettings;
            }
        }

        private V2Ray.StreamSettings.TlsSettings XtlsSettings
        {
            get
            {
                V2Ray.StreamSettings.TlsSettings xtlsSettings = null;

                if (General.streamSecurity == Global.StreamSecurity.XTLS)
                {
                    xtlsSettings = new V2Ray.StreamSettings.TlsSettings() {
                        allowInsecure = false,
                        alpn = new[] { General.alpn },
                        fingerprint = General.fingerprint
                    };

                    if (!string.IsNullOrWhiteSpace(General.sni))
                        xtlsSettings.serverName = General.sni;
                    else if (!string.IsNullOrWhiteSpace(General.requestHost))
                        xtlsSettings.serverName = General.requestHost;
                }

                return xtlsSettings;
            }
        }

        private V2Ray.StreamSettings.WsSettings WsSettings
        {
            get
            {
                V2Ray.StreamSettings.WsSettings wsSettings = null;

                if (General.streamNetwork == Global.StreamNetwork.WS)
                {
                    wsSettings = new V2Ray.StreamSettings.WsSettings();

                    if (!string.IsNullOrWhiteSpace(General.requestHost))
                        wsSettings.headers = new V2Ray.StreamSettings.WsSettings.Headers() {
                            Host = General.requestHost
                        };
                    
                    if (!string.IsNullOrWhiteSpace(General.path))
                        wsSettings.path = General.path;
                }

                return wsSettings;
            }
        }

        private V2Ray.StreamSettings.HttpSettings HttpSettings
        {
            get
            {
                V2Ray.StreamSettings.HttpSettings httpSettings = null;

                if (General.streamNetwork == Global.StreamNetwork.H2)
                {
                    httpSettings = new V2Ray.StreamSettings.HttpSettings() {
                        path = General.path
                    };

                    if (!string.IsNullOrWhiteSpace(General.requestHost))
                        httpSettings.host = new[] { General.requestHost };
                }

                return httpSettings;
            }
        }

        private V2Ray.StreamSettings.QuicSettings QuicSettings
        {
            get
            {
                V2Ray.StreamSettings.QuicSettings quicSettings = null;

                if (General.streamNetwork == Global.StreamNetwork.QUIC)
                {
                    quicSettings = new V2Ray.StreamSettings.QuicSettings() {
                        security = General.requestHost,
                        key = General.path,
                        header = new V2Ray.Header() {
                            type = General.headerType
                        }
                    };
                }

                return quicSettings;
            }
        }

        private V2Ray.StreamSettings.TcpSettings TcpSettings
        {
            get
            {
                V2Ray.StreamSettings.TcpSettings tcpSettings = null;

                if (General.headerType == "http")
                {
                    tcpSettings = new V2Ray.StreamSettings.TcpSettings() {
                        header = new V2Ray.Header() {
                            type = General.headerType,
                            request = GetRequest()
                        }
                    };
                }

                return tcpSettings;

                object GetRequest()
                {
                    string request = @"
                        {'version':'1.1',
                        'method':'GET',
                        'path':[$requestPath$],
                        'headers':{'Host':[$requestHost$],
                        'User-Agent':'',
                        'Accept-Encoding':['gzip, deflate'],
                        'Connection':['keep-alive'],
                        'Pragma':'no-cache'}}
                    ";
                    
                    string[] hostArray = General.requestHost.Split(',');
                    string hostsString = string.Join("','", hostArray);
                    request = request.Replace("$requestHost$", $"'{hostsString}'");

                    string httpPath = "/";
                    if (!string.IsNullOrEmpty(General.path))
                    {
                        string[] pathArray = General.path.Split(',');
                        httpPath = string.Join("','", pathArray);
                    }

                    request = request.Replace("$requestPath$", $"\"{httpPath}\"");
                    return JsonUtility.ConvertFromJson<object>(request);
                }
            }
        }
    }
}