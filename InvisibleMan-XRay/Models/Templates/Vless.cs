using System;
using System.Web;
using System.Collections.Specialized;

namespace InvisibleManXRay.Models.Templates
{
    using Values;

    public class Vless : Template
    {
        public class Data
        {
            public Uri uri;
            public NameValueCollection query;

            public Data(string url)
            {
                this.uri = new Uri(url);
                this.query = HttpUtility.ParseQueryString(uri.Query);
            }
        }

        private Data data;

        public override Status FetchDataFromLink(string link) 
        {
            data = new Data(link);

            if (IsInvalidLink())
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.INVALID_CONFIG,
                    content: Message.UNSUPPORTED_LINK
                );

            return new Status(Code.SUCCESS, SubCode.SUCCESS, null);

            bool IsInvalidLink() => data.query.Count == 0;
        }

        protected override Adapter Adapter
        {
            get
            {
                Adapter adapter = new Adapter() {
                    type = "vless",
                    remark = data.uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
                    address = data.uri.IdnHost,
                    port = data.uri.Port,
                    id = data.uri.UserInfo,
                    security = data.query["encryption"] ?? "none",
                    streamNetwork = data.query["type"] ?? "tcp",
                    streamSecurity = data.query["security"] ?? "",
                    flow = data.query["flow"] ?? "",
                    sni = data.query["sni"] ?? "",
                    alpn = HttpUtility.UrlDecode(data.query["alpn"] ?? ""),
                    fingerprint = HttpUtility.UrlDecode(data.query["fp"] ?? ""),
                };

                switch (adapter.streamNetwork)
                {
                    case "tcp":
                        adapter.headerType = data.query["headerType"] ?? "none";
                        adapter.requestHost = HttpUtility.UrlDecode(data.query["host"] ?? "");
                        break;
                    case "kcp":
                        adapter.headerType = data.query["headerType"] ?? "none";
                        adapter.path = HttpUtility.UrlDecode(data.query["seed"] ?? "");
                        break;
                    case "ws":
                        adapter.requestHost = HttpUtility.UrlDecode(data.query["host"] ?? "");
                        adapter.path = HttpUtility.UrlDecode(data.query["path"] ?? "/");
                        break;
                    case "http":
                    case "h2":
                        adapter.streamNetwork = "h2";
                        adapter.requestHost = HttpUtility.UrlDecode(data.query["host"] ?? "");
                        adapter.path = HttpUtility.UrlDecode(data.query["path"] ?? "/");
                        break;
                    case "quic":
                        adapter.headerType = data.query["headerType"] ?? "none";
                        adapter.requestHost = data.query["quicSecurity"] ?? "none";
                        adapter.path = HttpUtility.UrlDecode(data.query["key"] ?? "");
                        break;
                    case "grpc":
                        adapter.path = HttpUtility.UrlDecode(data.query["serviceName"] ?? "");
                        adapter.headerType = HttpUtility.UrlDecode(data.query["mode"] ?? "gun");
                        break;
                    default:
                        break;
                }

                return adapter;
            }
        }

        protected override V2Ray.Outbound.Settings OutboundSettings
        {
            get
            {
                if (Adapter.streamSecurity == "xtls")
                {
                    if (string.IsNullOrEmpty(Adapter.flow))
                        Adapter.flow = "xtls-rprx-origin";
                    else
                        Adapter.flow = Adapter.flow.Replace("splice", "direct");
                }

                return new V2Ray.Outbound.Settings() {
                    vnext = new V2Ray.Outbound.Settings.Vnext[] {
                        new V2Ray.Outbound.Settings.Vnext() {
                            address = Adapter.address,
                            port = Adapter.port,
                            users = new V2Ray.Outbound.Settings.Vnext.User[] {
                                new V2Ray.Outbound.Settings.Vnext.User() {
                                    id = Adapter.id,
                                    flow = Adapter.flow,
                                    email = Global.DEFAULT_EMAIL,
                                    encryption = Adapter.security
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}