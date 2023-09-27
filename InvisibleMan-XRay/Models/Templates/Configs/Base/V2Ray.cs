using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Templates.Configs
{
    public class V2Ray
    {
        public Log log;
        public Inbound[] inbounds;
        public Outbound[] outbounds;
        public Stats stats;
        public Api api;
        public Policy policy;
        public object dns;
        public Routing routing;

        public class Log
        {
            public string access;
            public string error;
            public string loglevel;
        }

        public class Inbound
        {
            public string tag;
            public int port;
            public string listen;
            public string protocol;
            public Sniffing sniffing;
            public Settings settings;
            public StreamSettings streamSettings;

            public class Settings
            {
                public string auth;
                public bool udp;
                public string ip;
                public string address;
                public Client[] clients;
                public string decryption;
                public bool allowTransparent;
                public Account[] accounts;
                
                public class Client
                {
                    public string id;
                    public int alterId;
                    public string email;
                    public string security;
                    public string encryption;
                    public string flow;
                }

                public class Account
                {
                    public string user;
                    public string pass;
                }
            }

            public class Sniffing
            {
                public bool enabled;
                public string[] destOverride;
            }
        }

        public class Outbound
        {
            public string tag;
            public string protocol;
            public Settings settings;
            public StreamSettings streamSettings;
            public Mux mux;

            public class Settings
            {
                public Vnext[] vnext;
                public Server[] servers;
                public Response response;
                public string domainStrategy;
                public int userLevel;

                public class Vnext
                {
                    public string address;
                    public int port;
                    public User[] users;

                    public class User
                    {
                        public string id;
                        public int alterId;
                        public string email;
                        public string security;
                        public string encryption;
                        public string flow;
                    }
                }

                public class Server
                {
                    public string email;
                    public string address;
                    public string method;
                    public bool ota;
                    public string password;
                    public int port;
                    public int level;
                    public string flow;
                    public SocksUser[] users;

                    public class SocksUser
                    {
                        public string user;
                        public string pass;
                        public int level;
                    }
                }

                public class Response
                {
                    public string type;
                }
            }

            public class Mux
            {
                public bool enabled;
                public int concurrency;
            }
        }

        public class StreamSettings
        {
            public string network;
            public string security;
            public TlsSettings tlsSettings;
            public TcpSettings tcpSettings;
            public KcpSettings kcpSettings;
            public WsSettings wsSettings;
            public HttpSettings httpSettings;
            public QuicSettings quicSettings;
            public TlsSettings xtlsSettings;
            public GrpcSettings grpcSettings;
            public RealitySettings realitySettings;

            public class TlsSettings
            {
                public bool allowInsecure;
                public string serverName;
                public string[] alpn;
                public string fingerprint;
            }

            public class TcpSettings
            {
                public Header header;
            }

            public class KcpSettings
            {
                public int mtu;
                public int tti;
                public int uplinkCapacity;
                public int downlinkCapacity;
                public bool congestion;
                public int readBufferSize;
                public int writeBufferSize;
                public Header header;
                public string seed;
            }

            public class WsSettings
            {
                public string path;
                public Headers headers;

                public class Headers
                {
                    public string Host;

                    [JsonProperty("User-Agent")]
                    public string UserAgent;
                }
            }

            public class HttpSettings
            {
                public string path;
                public string[] host;
            }

            public class QuicSettings
            {
                public string security;
                public string key;
                public Header header;
            }

            public class GrpcSettings
            {
                public string serviceName;
                public bool multiMode;
                public int idle_timeout;
                public int health_check_timeout;
                public bool permit_without_stream;
                public int initial_windows_size;
            }

            public class RealitySettings
            {
                public bool show;
                public string fingerprint;
                public string serverName;
                public string publicKey;
                public string shortId;
                public string spiderX;
            }
        }

        public class Header
        {
            public string type;
            public object request;
            public object response;
        }

        public class Stats
        {

        }

        public class Api
        {
            public string tag;
            public string[] services;
        }

        public class Policy
        {
            public SystemPolicy system;

            public class SystemPolicy
            {
                public bool statsOutboundUplink;
                public bool statsOutboundDownlink;
            }
        }

        public class Routing
        {
            public string domainStrategy;
            public string domainMatcher;
            public Rule[] rules;

            public class Rule
            {
                public string id;
                public string type;
                public string port;
                public string[] inboundTag;
                public string outboundTag;
                public string[] ip;
                public string[] domain;
                public string[] protocol;
                public bool enabled = true;

            }
        }
    }
}