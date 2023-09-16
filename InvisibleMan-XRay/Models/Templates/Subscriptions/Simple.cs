using System.Net;

namespace InvisibleManXRay.Models.Templates.Subscriptions
{
    using Values;

    public class Simple : Template
    {
        public override Status FetchDataFromLink(string link)
        {
            try
            {
                WebClient webClient = new WebClient();
                Data = webClient.DownloadString(link);

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch
            {
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.INVALID_CONFIG,
                    content: Message.INVALID_CONFIG
                );
            }
        }
    }
}