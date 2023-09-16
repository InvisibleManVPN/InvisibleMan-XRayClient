using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers.Templates
{
    using Models;
    using Models.Templates.Subscriptions;
    using Values;

    public class SubscriptionTemplate : ITemplate
    {
        private List<Type> templates;
        private Func<string, Status> convertConfigLinkToV2Ray;

        public SubscriptionTemplate()
        {
            this.templates = new List<Type>();
        }

        public void Setup(Func<string, Status> convertConfigLinkToV2Ray)
        {
            this.convertConfigLinkToV2Ray = convertConfigLinkToV2Ray;
        }

        public void RegisterTemplates()
        {
            templates.Add(typeof(Simple));
            templates.Add(typeof(Jwt));
        }

        public Status ConvertLinkToSubscription(string remark, string link)
        {
            Template template = FindTemplate();
            if (template == null)
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: Message.UNSUPPORTED_LINK
                );

            Status fetchingStatus = template.FetchDataFromLink(link);
            if (fetchingStatus.Code == Code.ERROR)
                return fetchingStatus;
            
            List<string[]> v2RayList = template.ConvertToV2RayList(convertConfigLinkToV2Ray);

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: new string[] { remark, JsonConvert.SerializeObject(v2RayList) }
            );

            Template FindTemplate()
            {
                foreach(Type type in templates)
                {
                    Template template = Activator.CreateInstance(type) as Template;
                    Status fetchingStatus = template.FetchDataFromLink(link);

                    if (fetchingStatus.Code == Code.SUCCESS)
                        return template;
                }

                return null;
            }
        }
    }
}