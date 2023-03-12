using System;
using System.Text;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Templates
{
    using Values;

    public class Vmess : Template
    {
        public class Data
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
        }

        private Data data;

        public Vmess()
        {
            this.data = new Data();
        }

        public override Status FetchDataFromLink(string link)
        {
            try
            {
                MapDecodedLinkToData(decodedString: DecodeBase64Link());
                if (IsInvalidLink())
                    throw new Exception();

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch(Exception)
            {
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.INVALID_CONFIG,
                    content: Message.INVALID_CONFIG
                );
            }

            string DecodeBase64Link()
            {
                string base64String = link.Replace("vmess://", "");
                byte[] dataBytes = Convert.FromBase64String(base64String);
                
                return Encoding.UTF8.GetString(dataBytes);
            }

            void MapDecodedLinkToData(string decodedString)
            {
                 data = JsonConvert.DeserializeObject<Data>(decodedString);
            }

            bool IsInvalidLink() => data == null;
        }

        protected override Adapter Adapter => new Adapter() {
            type = "vmess",
            headerType = data.type,
            version = !string.IsNullOrEmpty(data.v) ? int.Parse(data.v) : 0,
            remark = data.ps,
            address = data.add,
            port = !string.IsNullOrEmpty(data.v)? int.Parse(data.port) : 0,
            id = data.id,
            alterId = !string.IsNullOrEmpty(data.v) ? int.Parse(data.aid) : 0,
            security = data.scy,
            requestHost = data.host,
            path = data.path,
            streamNetwork = data.net,
            streamSecurity = data.tls,
            sni = data.sni,
            alpn = data.alpn,
            fingerprint = data.fp
        };

        protected override V2Ray.Outbound.Settings OutboundSettings => new V2Ray.Outbound.Settings() {
            vnext = new V2Ray.Outbound.Settings.Vnext[] {
                new V2Ray.Outbound.Settings.Vnext() {
                    address = Adapter.address,
                    port = Adapter.port,
                    users = new V2Ray.Outbound.Settings.Vnext.User[] {
                        new V2Ray.Outbound.Settings.Vnext.User() {
                            id = Adapter.id,
                            alterId = Adapter.alterId,
                            email = Global.DEFAULT_EMAIL,
                            security = Adapter.security,
                        }
                    }
                }
            }
        };
    }
}