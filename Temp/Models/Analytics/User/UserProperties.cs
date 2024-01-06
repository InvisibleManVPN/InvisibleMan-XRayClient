using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Analytics.User
{
    public class UserProperties
    {
        [JsonProperty("customer_tier")]
        public UserTier CustomerTier;

        public UserProperties(UserTier customerTier)
        {
            this.CustomerTier = customerTier;
        }
    }
}