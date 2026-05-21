using System;
using System.Linq;

namespace InvisibleGorillaXRay.Android.Handlers.Tunnels
{
    using InvisibleGorillaXRay.Core;
    using InvisibleGorillaXRay.Handlers;
    using InvisibleGorillaXRay.Android.Handlers.Settings;
    using InvisibleGorillaXRay.Android.Services;
    using InvisibleGorillaXRay.Handlers.Tunnels;
    using InvisibleGorillaXRay.Models;
    using InvisibleGorillaXRay.Services;
    using InvisibleGorillaXRay.Values;

    public sealed class AndroidTunnel : ITunnel
    {
        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public Status Enable(string ip, int port, string address, string server, string dns, LocalProxyCredentials localProxyCredentials)
        {
            DiagnosticLog.Write(
                "AndroidTunnel",
                $"TUN mode requested for proxy={ip}:{port}, address={address}, server={server}, dns={dns}, authEnabled={localProxyCredentials?.HasValue == true}");

            (AppRulesMode appRulesMode, string[] appPackages) = GetAppRulePackages();

            Status startStatus = AndroidVpnServiceController.Start(new AndroidVpnStartOptions
            {
                ProxyPort = port,
                ProxyUsername = localProxyCredentials?.Username ?? string.Empty,
                ProxyPassword = localProxyCredentials?.Password ?? string.Empty,
                UdpEnabled = true,
                TunAddress = address,
                Dns = dns,
                SessionName = "Invisible Gorilla XRay",
                AppRulesMode = appRulesMode,
                AppPackages = appPackages
            });

            if (startStatus.Code == Code.ERROR)
            {
                string detail = startStatus.Content?.ToString()
                    ?? AndroidVpnServiceController.LastError
                    ?? string.Empty;

                if (detail.Contains("permission", StringComparison.OrdinalIgnoreCase))
                {
                    return new Status(
                        code: Code.ERROR,
                        subCode: SubCode.CANT_TUNNEL,
                        content: LocalizationService.GetTerm("Lang.Android.Status.VpnPermissionDenied"));
                }

                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.CANT_TUNNEL,
                    content: string.Format(
                        LocalizationService.GetTerm("Lang.Android.Status.VpnStartFailed"),
                        detail));
            }

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: string.Empty);
        }

        public void Disable()
        {
            DiagnosticLog.Write("AndroidTunnel", "Disable requested");
            AndroidVpnServiceController.Stop();
        }

        public void Cancel()
        {
            DiagnosticLog.Write("AndroidTunnel", "Cancel requested");
            AndroidVpnServiceController.Stop();
        }

        private static (AppRulesMode Mode, string[] Packages) GetAppRulePackages()
        {
            SettingsHandler settingsHandler = new(() => new AndroidStartup());
            UserSettings settings = settingsHandler.UserSettings;

            string configPath = settings.GetCurrentConfigPath();
            string boundTemplateId = settings.GetBoundAppRuleTemplateId();
            AppRulesMode mode = settings.GetEffectiveAppRulesMode();

            DiagnosticLog.Write($"[AppRules] GetAppRulePackages: configPath={configPath}, boundTemplate={boundTemplateId}, mode={mode}");

            if (mode == AppRulesMode.ALL_APPS)
            {
                DiagnosticLog.Write("[AppRules] Mode=ALL_APPS → no packages to pass");
                return (mode, Array.Empty<string>());
            }

            string[] packages = settings.GetEffectiveEnabledAppRules()
                .Select(rule => rule.AppId?.Trim())
                .Where(appId => !string.IsNullOrWhiteSpace(appId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()!;

            DiagnosticLog.Write($"[AppRules] Mode={mode}, packages count={packages.Length}: [{string.Join(", ", packages)}]");

            return (mode, packages);
        }
    }
}
