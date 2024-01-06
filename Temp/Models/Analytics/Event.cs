using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace InvisibleManXRay.Models.Analytics
{
    public class Event
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("params")]
        public Dictionary<string, object> Params;

        public Event(string eventName, Param[] eventParams)
        {
            this.Name = eventName;
            this.Params = eventParams.ToDictionary(p => p.Key, p => p.Value);
        }
    }
}