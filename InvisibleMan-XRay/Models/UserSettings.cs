using System.ComponentModel;
using Newtonsoft.Json;

namespace InvisibleManXRay.Models
{
    public enum Mode { PROXY, TUN }

    public enum Protocol { HTTP, SOCKS }

    public enum LogLevel { NONE, DEBUG, INFO, WARNING, ERROR }

    public class UserSettings
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(0)]
        public int CurrentConfigIndex;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Mode.PROXY)]
        public Mode Mode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(Protocol.HTTP)]
        public Protocol Protocol;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(true)]
        public bool IsUdpEnable;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(false)]
        public bool IsRunAtStartup;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10801)]
        public int ProxyPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10802)]
        public int TunPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(10803)]
        public int TestPort;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("10.0.236.10")]
        public string TunIp;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("8.8.8.8")]
        public string Dns;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(LogLevel.NONE)]
        public LogLevel LogLevel;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue("./Logs")]
        public string LogPath;

        public UserSettings()
        {
            this.CurrentConfigIndex = 0;
            this.Mode = Mode.PROXY;
            this.Protocol = Protocol.HTTP;
            this.IsUdpEnable = true;
            this.IsRunAtStartup = false;
            this.TunIp = "10.0.236.10";
            this.ProxyPort = 10801;
            this.TunPort = 10802;
            this.TestPort = 10803;
            this.Dns = "8.8.8.8";
            this.LogLevel = LogLevel.NONE;
            this.LogPath = "./Logs";
        }

        public UserSettings(
            Mode mode,
            Protocol protocol,
            LogLevel logLevel,
            bool isUdpEnable,
            bool isRunAtStartup,
            int proxyPort,
            int tunPort,
            int testPort,
            string tunIp,
            string dns,
            string logPath
        )
        {
            this.Mode = mode;
            this.Protocol = protocol;
            this.LogLevel = logLevel;
            this.IsUdpEnable = isUdpEnable;
            this.IsRunAtStartup = isRunAtStartup;
            this.ProxyPort = proxyPort;
            this.TunPort = tunPort;
            this.TestPort = testPort;
            this.TunIp = tunIp;
            this.Dns = dns;
            this.LogPath = logPath;
        }

        public int GetCurrentConfigIndex() => CurrentConfigIndex;

        public Mode GetMode() => Mode;

        public Protocol GetProtocol() => Protocol;

        public bool GetUdpEnabled() => IsUdpEnable;

        public bool GetRunAtStartupEnabled() => IsRunAtStartup;

        public int GetProxyPort() => ProxyPort;

        public int GetTunPort() => TunPort;

        public int GetTestPort() => TestPort;

        public string GetTunIp() => TunIp;

        public string GetDns() => Dns;

        public LogLevel GetLogLevel() => LogLevel;

        public string GetLogPath() => LogPath;
    }
}