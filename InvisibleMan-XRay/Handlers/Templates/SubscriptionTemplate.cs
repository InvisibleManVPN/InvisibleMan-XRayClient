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

        public SubscriptionTemplate()
        {
            this.templates = new List<Type>();
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
            
            List<Models.Templates.Configs.V2Ray> v2RayList = template.ConvertToV2RayList();

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