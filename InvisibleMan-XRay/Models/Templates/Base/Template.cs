namespace InvisibleManXRay.Models.Templates
{
    using Utilities;
    using Values;

    public abstract class Template
    {
        private V2Ray v2Ray;
        protected abstract Adapter Adapter { get; }
        protected abstract V2Ray.Outbound.Settings OutboundSettings { get; }

        public abstract Status FetchDataFromLink(string link);

        public string GetRemark()
        {
            return AllowJustAsciiCharacters(RemoveInvalidFileCharacters());

            string RemoveInvalidFileCharacters()
            {
                return string.Concat(Adapter.remark.Split(
                    System.IO.Path.GetInvalidFileNameChars()
                ));
            }

            string AllowJustAsciiCharacters(string remark)
            {
                return System.Text.RegularExpressions.Regex.Replace(
                    input: remark, 
                    pattern: @"[^\u0000-\u007F]+", 
                    replacement: string.Empty
                ).Trim();
            }
        }
        
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
                protocol = Adapter.type,
                settings = OutboundSettings,
                streamSettings = new V2Ray.StreamSettings() {
                    network = Adapter.streamNetwork,
                    security = Adapter.streamSecurity,
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

                if (Adapter.streamSecurity == Global.StreamSecurity.TLS)
                {
                    tlsSettings = new V2Ray.StreamSettings.TlsSettings() {
                        allowInsecure = false,
                        alpn = new[] { Adapter.alpn },
                        fingerprint = Adapter.fingerprint
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.sni))
                        tlsSettings.serverName = Adapter.sni;
                    else if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        tlsSettings.serverName = Adapter.requestHost;
                }

                return tlsSettings;
            }
        }

        private V2Ray.StreamSettings.TlsSettings XtlsSettings
        {
            get
            {
                V2Ray.StreamSettings.TlsSettings xtlsSettings = null;

                if (Adapter.streamSecurity == Global.StreamSecurity.XTLS)
                {
                    xtlsSettings = new V2Ray.StreamSettings.TlsSettings() {
                        allowInsecure = false,
                        alpn = new[] { Adapter.alpn },
                        fingerprint = Adapter.fingerprint
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.sni))
                        xtlsSettings.serverName = Adapter.sni;
                    else if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        xtlsSettings.serverName = Adapter.requestHost;
                }

                return xtlsSettings;
            }
        }

        private V2Ray.StreamSettings.WsSettings WsSettings
        {
            get
            {
                V2Ray.StreamSettings.WsSettings wsSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.WS)
                {
                    wsSettings = new V2Ray.StreamSettings.WsSettings();

                    if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        wsSettings.headers = new V2Ray.StreamSettings.WsSettings.Headers() {
                            Host = Adapter.requestHost
                        };
                    
                    if (!string.IsNullOrWhiteSpace(Adapter.path))
                        wsSettings.path = Adapter.path;
                }

                return wsSettings;
            }
        }

        private V2Ray.StreamSettings.HttpSettings HttpSettings
        {
            get
            {
                V2Ray.StreamSettings.HttpSettings httpSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.H2)
                {
                    httpSettings = new V2Ray.StreamSettings.HttpSettings() {
                        path = Adapter.path
                    };

                    if (!string.IsNullOrWhiteSpace(Adapter.requestHost))
                        httpSettings.host = new[] { Adapter.requestHost };
                }

                return httpSettings;
            }
        }

        private V2Ray.StreamSettings.QuicSettings QuicSettings
        {
            get
            {
                V2Ray.StreamSettings.QuicSettings quicSettings = null;

                if (Adapter.streamNetwork == Global.StreamNetwork.QUIC)
                {
                    quicSettings = new V2Ray.StreamSettings.QuicSettings() {
                        security = Adapter.requestHost,
                        key = Adapter.path,
                        header = new V2Ray.Header() {
                            type = Adapter.headerType
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

                if (Adapter.headerType == "http")
                {
                    tcpSettings = new V2Ray.StreamSettings.TcpSettings() {
                        header = new V2Ray.Header() {
                            type = Adapter.headerType,
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
                    
                    string[] hostArray = Adapter.requestHost.Split(',');
                    string hostsString = string.Join("','", hostArray);
                    request = request.Replace("$requestHost$", $"'{hostsString}'");

                    string httpPath = "/";
                    if (!string.IsNullOrEmpty(Adapter.path))
                    {
                        string[] pathArray = Adapter.path.Split(',');
                        httpPath = string.Join("','", pathArray);
                    }

                    request = request.Replace("$requestPath$", $"\"{httpPath}\"");
                    return JsonUtility.ConvertFromJson<object>(request);
                }
            }
        }
    }
}