using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models
{
    public enum Mode { PROXY, TUN }

    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public int CurrentConfigIndex;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Mode.PROXY)]
        public Mode Mode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("10.0.236.10")]
        public string TunIp;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("8.8.8.8")]
        public string Dns;

        public UserSettings()
        {
            this.CurrentConfigIndex = 0;
            this.Mode = Mode.PROXY;
            this.TunIp = "10.0.236.10";
            this.Dns = "8.8.8.8";
        }

        public int GetCurrentConfigIndex() => CurrentConfigIndex;

        public Mode GetMode() => Mode;

        public string GetTunIp() => TunIp;

        public string GetDns() => Dns;
    }
}