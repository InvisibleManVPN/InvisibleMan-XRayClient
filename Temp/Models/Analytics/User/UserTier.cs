using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Analytics.User
{
    public class UserTier
    {
        [JsonProperty("value")]
        public string Value;

        public UserTier(string value)
        {
            this.Value = value;
        }
    }
}