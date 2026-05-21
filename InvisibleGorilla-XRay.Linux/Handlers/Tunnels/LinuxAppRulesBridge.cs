using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Linux.Handlers.Tunnels
{
    using InvisibleGorillaXRay.Core;
    using InvisibleGorillaXRay.Models;

    internal sealed class LinuxAppRulesBridgeConfig
    {
        public string Mode { get; init; } = AppRulesMode.ALL_APPS.ToString();
        public string SocksUri { get; init; } = string.Empty;
        public int SocksPort { get; init; }
        public string SocksUsername { get; init; } = string.Empty;
        public string SocksPassword { get; init; } = string.Empty;
        public string TunnelAddress { get; init; } = string.Empty;
        public string Dns { get; init; } = string.Empty;
        public List<string> AppIds { get; init; } = new();
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Persists the user's app rules selection in a JSON descriptor next to the TUN
    /// helper. The Linux equivalent of MacAppRulesBridge.
    ///
    /// Kernel-level enforcement (via cgroups + iptables OUTPUT marking) is intentionally
    /// out of scope for the first cut; this descriptor is kept stable so a privileged
    /// helper can pick it up later without breaking the contract.
    /// </summary>
    internal static class LinuxAppRulesBridge
    {
        private static string ConfigPath => Path.Combine(Values.Directory.DATA_TUN, "linux-transparent-proxy-config.json");

        public static Status Prepare(UserSettings settings, int socksPort, string tunnelAddress, string dns, LocalProxyCredentials localProxyCredentials)
        {
            try
            {
                Values.Directory.EnsureWritableDirectories();
                AppRulesMode mode = settings.GetEffectiveAppRulesMode();
                List<string> appIds = settings.GetEffectiveEnabledAppRules()
                    .Select(rule => rule.AppId?.Trim())
                    .Where(appId => !string.IsNullOrWhiteSpace(appId))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()!;

                LinuxAppRulesBridgeConfig config = new()
                {
                    Mode = mode.ToString(),
                    SocksUri = localProxyCredentials?.HasValue == true
                        ? localProxyCredentials.BuildSocks5Uri("127.0.0.1", socksPort)
                        : $"socks5://127.0.0.1:{socksPort}",
                    SocksPort = socksPort,
                    SocksUsername = localProxyCredentials?.Username ?? string.Empty,
                    SocksPassword = localProxyCredentials?.Password ?? string.Empty,
                    TunnelAddress = tunnelAddress ?? string.Empty,
                    Dns = dns ?? string.Empty,
                    AppIds = appIds
                };

                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                DiagnosticLog.Write(
                    "LinuxAppRulesBridge",
                    $"Prepared transparent proxy descriptor with mode={mode} and {appIds.Count} app ids at {ConfigPath}");

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("LinuxAppRulesBridge.Prepare", ex);
                return new Status(Code.ERROR, SubCode.CANT_TUNNEL, $"Failed to prepare Linux app rules: {ex.Message}");
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    File.Delete(ConfigPath);
            }
            catch { }
        }
    }
}
