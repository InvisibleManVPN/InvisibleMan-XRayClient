namespace InvisibleManXRay.Models.Templates
{
    using Values;

    public class Vmess : Template
    {
        public string v;
        public string ps;
        public string add;
        public string port;
        public string id;
        public string aid;
        public string scy;
        public string net;
        public string type;
        public string host;
        public string path;
        public string tls;
        public string sni;
        public string alpn;
        public string fp;

        protected override General General => new General() {
            type = "vmess",
            headerType = type,
            version = !string.IsNullOrEmpty(v) ? int.Parse(v) : 0,
            remark = ps,
            address = add,
            port = !string.IsNullOrEmpty(v)? int.Parse(port) : 0,
            id = id,
            alterId = !string.IsNullOrEmpty(v) ? int.Parse(aid) : 0,
            security = scy,
            requestHost = host,
            path = path,
            streamNetwork = net,
            streamSecurity = tls,
            sni = sni,
            alpn = alpn,
            fingerprint = fp
        };

        protected override V2Ray.Outbound.Settings OutboundSettings => new V2Ray.Outbound.Settings() {
            vnext = new V2Ray.Outbound.Settings.Vnext[] {
                new V2Ray.Outbound.Settings.Vnext() {
                    address = General.address,
                    port = General.port,
                    users = new V2Ray.Outbound.Settings.Vnext.User[] {
                        new V2Ray.Outbound.Settings.Vnext.User() {
                            id = General.id,
                            alterId = General.alterId,
                            email = Global.DEFAULT_EMAIL,
                            security = General.security,
                        }
                    }
                }
            }
        };
    }
}