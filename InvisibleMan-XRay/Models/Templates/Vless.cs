using System;
using System.Web;
using System.Collections.Specialized;

namespace InvisibleManXRay.Models.Templates
{
    using Values;

    public class Vless : Template
    {
        public string url;
        private Uri uri => new Uri(url);
        private NameValueCollection query => HttpUtility.ParseQueryString(uri.Query);

        protected override General General
        {
            get
            {
                General general = new General() {
                    type = "vless",
                    remark = uri.GetComponents(UriComponents.Fragment, UriFormat.Unescaped),
                    address = uri.IdnHost,
                    port = uri.Port,
                    id = uri.UserInfo,
                    security = query["encryption"] ?? "none",
                    streamNetwork = query["type"] ?? "tcp",
                    streamSecurity = query["security"] ?? "",
                    flow = query["flow"] ?? "",
                    sni = query["sni"] ?? "",
                    alpn = HttpUtility.UrlDecode(query["alpn"] ?? ""),
                    fingerprint = HttpUtility.UrlDecode(query["fp"] ?? ""),
                };

                switch (general.streamNetwork)
                {
                    case "tcp":
                        general.headerType = query["headerType"] ?? "none";
                        general.requestHost = HttpUtility.UrlDecode(query["host"] ?? "");
                        break;
                    case "kcp":
                        general.headerType = query["headerType"] ?? "none";
                        general.path = HttpUtility.UrlDecode(query["seed"] ?? "");
                        break;
                    case "ws":
                        general.requestHost = HttpUtility.UrlDecode(query["host"] ?? "");
                        general.path = HttpUtility.UrlDecode(query["path"] ?? "/");
                        break;
                    case "http":
                    case "h2":
                        general.streamNetwork = "h2";
                        general.requestHost = HttpUtility.UrlDecode(query["host"] ?? "");
                        general.path = HttpUtility.UrlDecode(query["path"] ?? "/");
                        break;
                    case "quic":
                        general.headerType = query["headerType"] ?? "none";
                        general.requestHost = query["quicSecurity"] ?? "none";
                        general.path = HttpUtility.UrlDecode(query["key"] ?? "");
                        break;
                    case "grpc":
                        general.path = HttpUtility.UrlDecode(query["serviceName"] ?? "");
                        general.headerType = HttpUtility.UrlDecode(query["mode"] ?? "gun");
                        break;
                    default:
                        break;
                }

                return general;
            }
        }

        protected override V2Ray.Outbound.Settings OutboundSettings
        {
            get
            {
                if (General.streamSecurity == "xtls")
                {
                    if (string.IsNullOrEmpty(General.flow))
                        General.flow = "xtls-rprx-origin";
                    else
                        General.flow = General.flow.Replace("splice", "direct");
                }

                return new V2Ray.Outbound.Settings() {
                    vnext = new V2Ray.Outbound.Settings.Vnext[] {
                        new V2Ray.Outbound.Settings.Vnext() {
                            address = General.address,
                            port = General.port,
                            users = new V2Ray.Outbound.Settings.Vnext.User[] {
                                new V2Ray.Outbound.Settings.Vnext.User() {
                                    id = General.id,
                                    flow = General.flow,
                                    email = Global.DEFAULT_EMAIL,
                                    encryption = General.security
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}