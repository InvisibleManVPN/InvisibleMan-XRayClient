using System.Collections.Generic;

namespace InvisibleManXRay.Models.Templates.Subscriptions
{
    using Models.Templates.Configs;

    public abstract class Template
    {
        public abstract Status FetchDataFromLink(string link);

        public List<V2Ray> ConvertToV2RayList()
        {
            return null;
        }
    }
}