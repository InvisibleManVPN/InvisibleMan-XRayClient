using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models.Analytics.User
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