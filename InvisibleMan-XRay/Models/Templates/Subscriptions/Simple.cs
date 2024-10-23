using System;
using System.Net;

namespace InvisibleManXRay.Models.Templates.Subscriptions
{
    using Values;

    public class Simple : Template
    {
        public override bool IsValid(string link)
        {
            return true;
        }

        public override Status FetchDataFromLink(string link)
        {
            try
            {
                WebClient webClient = new WebClient();
                Data = webClient.DownloadString(link);
                if (!IsAnyDataExisits())
                    throw new Exception();

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch
            {
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: LocalizationService.GetTerm(Localization.UNSUPPORTED_SUBSCRIPTION_LINK)
                );
            }
        }
    }
}