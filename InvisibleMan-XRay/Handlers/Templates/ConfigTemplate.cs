using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers.Templates
{
    using Models;
    using Models.Templates.Configs;
    using Values;

    public class ConfigTemplate : ITemplate
    {
        private Dictionary<string, Type> templates;

        public ConfigTemplate()
        {
            this.templates = new Dictionary<string, Type>();
        }

        public void RegisterTemplates()
        {
            templates.Add("vmess", typeof(Vmess));
            templates.Add("vless", typeof(Vless));
            templates.Add("trojan", typeof(Trojan));
            templates.Add("ss", typeof(Shadowsocks));
        }

        public Status ConverLinkToConfig(string link)
        {
            Template template = FindTemplate(type: FetchConfigType());
            if (template == null)
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: Message.UNSUPPORTED_CONFIG_LINK
                );

            Status fetchingStatus = template.FetchDataFromLink(link);
            if (fetchingStatus.Code == Code.ERROR)
                return fetchingStatus;

            V2Ray v2Ray = template.ConvertToV2Ray();
            string remark = template.GetValidRemark();

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: new string[] { remark, JsonConvert.SerializeObject(v2Ray) }
            );

            string FetchConfigType() => link.Split("://").First();

            Template FindTemplate(string type)
            {
                var template = templates.FirstOrDefault(
                    (element) => element.Key == type.ToLower()
                );

                if (template.Key == null)
                    return null;
                    
                return Activator.CreateInstance(template.Value) as Template;
            }
        }
    }
}