using Newtonsoft.Json;

namespace InvisibleManXRay.Models.Analytics
{
    using User;

    public class Payload
    {
        [JsonProperty("client_id")]
        public string ClientId;

        [JsonProperty("user_id")]
        public string UserId;

        [JsonProperty("user_properties")]
        public UserProperties UserProperties;

        [JsonProperty("events")]
        public Event[] Events;
    }
}