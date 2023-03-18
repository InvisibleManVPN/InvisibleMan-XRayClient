using System;
using System.Linq;
using System.Text;

namespace InvisibleManXRay.Models.Templates
{
    using Values;

    public class Shadowsocks : Template
    {
        public class Data
        {
            public Uri uri;
            public string security;
            public string id;
            public string remark;

            public Data(string url)
            {
                this.uri = new Uri(url);
                this.remark = url.Split("#")[1];
            }
        }

        private Data data;
        private readonly string[] validSecurity = new[] { 
            "aes-256-gcm", 
            "aes-128-gcm", 
            "chacha20-poly1305", 
            "chacha20-ietf-poly1305", 
            "xchacha20-poly1305", 
            "xchacha20-ietf-poly1305", 
            "none", 
            "plain", 
            "2022-blake3-aes-128-gcm", 
            "2022-blake3-aes-256-gcm", 
            "2022-blake3-chacha20-poly1305" 
        };

        public override Status FetchDataFromLink(string link)
        {
            try
            {
                data = new Data(link);
                FetchRemark();
                MapDecodedLinkToData(decodedString: DecodeBase64Link());

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
                string base64String = link.Split("@").FirstOrDefault().Replace("ss://", "");
                byte[] dataBytes = Convert.FromBase64String(base64String);
                
                return Encoding.UTF8.GetString(dataBytes);
            }

            void MapDecodedLinkToData(string decodedString)
            {
                string[] stringArray = decodedString.Split(":");
                data.security = stringArray[0];
                data.id = stringArray[1];
            }

            void FetchRemark()
            {
                data.remark = Uri.UnescapeDataString(data.uri.ToString().Split("#")[1]);
            }
        }

        protected override Adapter Adapter => new Adapter() {
            type = "shadowsocks",
            remark = data.remark,
            address = data.uri.IdnHost,
            port = data.uri.Port,
            id = data.id,
            security = data.security
        };

        protected override V2Ray.Outbound.Settings OutboundSettings => new V2Ray.Outbound.Settings() {
            servers = new V2Ray.Outbound.Settings.Server[] {
                new V2Ray.Outbound.Settings.Server() {
                    address = Adapter.address,
                    port = Adapter.port,
                    password = Adapter.id,
                    ota = false,
                    level = 1,
                    method = validSecurity.Contains(Adapter.security) ? Adapter.security : "none"
                }
            }
        };
    }
}