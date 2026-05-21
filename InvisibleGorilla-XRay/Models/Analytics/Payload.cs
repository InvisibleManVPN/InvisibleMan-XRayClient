using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Models.Analytics
{
    using User;

    public class Payload
    {
        [JsonProperty("client_id")]
        public string ClientId;

        [JsonProperty("user_properties")]
        public UserProperties UserProperties;

        [JsonProperty("events")]
        public Event[] Events;
    }
}