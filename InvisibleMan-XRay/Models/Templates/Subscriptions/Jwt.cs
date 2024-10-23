using System;
using System.Net;
using System.Linq;
using System.Text;

namespace InvisibleManXRay.Models.Templates.Subscriptions
{
    using Values;

    public class Jwt : Template
    {
        public override bool IsValid(string link)
        {
            string jwtToken = GetJwtToken(link);
            if (string.IsNullOrEmpty(jwtToken))
                return false;
            
            string[] tokens = GetTokenParts();
            if (tokens.Length != 3)
                return false;
            
            string token;
            
            token = TryDecodeAsBase64(tokens[0]);
            if (string.IsNullOrEmpty(token))
                return false;
            else if (!token.Contains("alg"))
                return false;
            
            token = TryDecodeAsBase64(tokens[1]);
            if(string.IsNullOrEmpty(token))
                return false;
            
            return true;

            string[] GetTokenParts() => jwtToken.Split(".");
            
            string TryDecodeAsBase64(string str)
            {
                try
                {
                    return Encoding.UTF8.GetString(
                        bytes: System.Convert.FromBase64String(str)
                    );
                }
                catch
                {
                    return null;
                }
            }
        }

        public override Status FetchDataFromLink(string link)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {GetJwtToken(link)}");
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

        private string GetJwtToken(string link) => link.Split("/").LastOrDefault();
    }
}