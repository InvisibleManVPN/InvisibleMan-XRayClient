using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Models;

namespace InvisibleGorillaXRay.Android.Services
{
    [Service(
        Name = "io.invisiblegorilla.xray.AndroidVpnService",
        Enabled = true,
        Exported = true,
        Permission = "android.permission.BIND_VPN_SERVICE",
        ForegroundServiceType = ForegroundService.TypeSpecialUse)]
    [IntentFilter(new[] { "android.net.VpnService" })]
    [MetaData("android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE", Value = "device_wide_vpn_tunnel_routing")]
    public class AndroidVpnService : VpnService
    {
        private const string ActionStart = "io.invisiblegorilla.xray.action.START_VPN";
        private const string ActionStop = "io.invisiblegorilla.xray.action.STOP_VPN";
        private const string ExtraProxyPort = "proxy_port";
        private const string ExtraProxyUsername = "proxy_username";
        private const string ExtraProxyPassword = "proxy_password";
        private const string ExtraUdpEnabled = "udp_enabled";
        private const string ExtraTunAddress = "tun_address";
        private const string ExtraDns = "dns";
        private const string ExtraSessionName = "session_name";
        private const string ExtraAppRulesMode = "app_rules_mode";
        private const string ExtraAppPackages = "app_packages";
        private const int DefaultMtu = 1500;
        private const string DefaultIpv6Address = "fdfe:dcba:9876::1";
        private const int DefaultIpv6PrefixLength = 126;
        private static readonly object SyncRoot = new();

        private Timer? healthTimer;

        internal static Intent CreateStartIntent(Context context, AndroidVpnStartOptions options)
        {
            Intent intent = new Intent(context, typeof(AndroidVpnService));
            intent.SetAction(ActionStart);
            intent.PutExtra(ExtraProxyPort, options.ProxyPort);
            intent.PutExtra(ExtraProxyUsername, options.ProxyUsername);
            intent.PutExtra(ExtraProxyPassword, options.ProxyPassword);
            intent.PutExtra(ExtraUdpEnabled, options.UdpEnabled);
            intent.PutExtra(ExtraTunAddress, options.TunAddress);
            intent.PutExtra(ExtraDns, options.Dns);
            intent.PutExtra(ExtraSessionName, options.SessionName);
            intent.PutExtra(ExtraAppRulesMode, (int)options.AppRulesMode);
            if (options.AppPackages.Count > 0)
                intent.PutStringArrayListExtra(ExtraAppPackages, new List<string>(options.AppPackages));
            return intent;
        }

        internal static Intent CreateStopIntent(Context context)
        {
            Intent intent = new Intent(context, typeof(AndroidVpnService));
            intent.SetAction(ActionStop);
            return intent;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            string? action = intent?.Action;
            DiagnosticLog.Write("AndroidVpnService", $"OnStartCommand action={action ?? "<null>"}");

            if (string.Equals(action, ActionStop, StringComparison.Ordinal))
            {
                AndroidConnectionNotificationManager.MarkStopping();
                _ = StopVpnAsync("Stop requested", startId);
                return StartCommandResult.NotSticky;
            }

            if (!string.Equals(action, ActionStart, StringComparison.Ordinal))
                return StartCommandResult.NotSticky;

            try
            {
                StartForegroundCompat();
                _ = StartVpnAsync(intent!, startId);
                return StartCommandResult.Sticky;
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.Start", ex);
                AndroidVpnServiceController.NotifyStartFailed(ex.Message);
                StopVpn(ex.Message);
                StopSelfResult(startId);
                return StartCommandResult.NotSticky;
            }
        }

        public override void OnDestroy()
        {
            DiagnosticLog.Write("AndroidVpnService", "Foreground VPN service destroyed");
            StopVpn("Android VPN service destroyed");
            base.OnDestroy();
        }

        private async Task StartVpnAsync(Intent intent, int startId)
        {
            try
            {
                await Task.Run(() => StartVpn(intent));
                AndroidVpnServiceController.NotifyStarted();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.StartAsync", ex);
                AndroidVpnServiceController.NotifyStartFailed(ex.Message);
                StopVpn(ex.Message);
                StopSelfResult(startId);
            }
        }

        private async Task StopVpnAsync(string reason, int startId)
        {
            try
            {
                await Task.Run(() => StopVpn(reason));
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.StopAsync", ex);
            }
            finally
            {
                StopSelfResult(startId);
            }
        }

        private void StartVpn(Intent intent)
        {
            if (Prepare(this) != null)
                throw new InvalidOperationException("Android VPN permission has not been granted.");

            int proxyPort = intent.GetIntExtra(ExtraProxyPort, 0);
            if (proxyPort <= 0)
                throw new InvalidOperationException("Android VPN proxy port is missing.");

            LocalProxyCredentials localProxyCredentials = new(
                username: intent.GetStringExtra(ExtraProxyUsername) ?? string.Empty,
                password: intent.GetStringExtra(ExtraProxyPassword) ?? string.Empty);
            if (!localProxyCredentials.HasValue)
                throw new InvalidOperationException("Android VPN local proxy credentials are missing.");

            bool udpEnabled = intent.GetBooleanExtra(ExtraUdpEnabled, true);
            string tunAddress = intent.GetStringExtra(ExtraTunAddress)?.Trim() ?? "10.0.236.10";
            string dns = intent.GetStringExtra(ExtraDns)?.Trim() ?? "8.8.8.8";
            string sessionName = intent.GetStringExtra(ExtraSessionName)?.Trim() ?? "Invisible Gorilla XRay";
            AppRulesMode appRulesMode = NormalizeAppRulesMode(intent.GetIntExtra(ExtraAppRulesMode, (int)AppRulesMode.ALL_APPS));
            string[] appPackages = intent.GetStringArrayListExtra(ExtraAppPackages)?
                .Where(packageName => !string.IsNullOrWhiteSpace(packageName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? Array.Empty<string>();

            lock (SyncRoot)
            {
                ResetVpnCore("Restarting Android VPN");

                Builder builder = new Builder(this)
                    .SetSession(sessionName)
                    .SetMtu(DefaultMtu)
                    .AddAddress(tunAddress, 32)
                    .AddRoute("0.0.0.0", 0);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                    builder.SetBlocking(true);

                foreach (string dnsServer in SplitDnsServers(dns))
                    builder.AddDnsServer(dnsServer);

                TryEnableIpv6(builder);
                ApplyApplicationRules(builder, appRulesMode, appPackages);

                DiagnosticLog.Write(
                    "AndroidVpnService",
                    $"Calling Builder.Establish() (mtu={DefaultMtu}, address={tunAddress}, mode={appRulesMode}, packages={appPackages.Length})...");
                ParcelFileDescriptor? tunInterface = builder.Establish();
                if (tunInterface == null)
                    throw new InvalidOperationException("Android VPN interface could not be established (Builder.Establish returned null). "
                        + "Verify the VPN consent dialog was accepted and that no other always-on VPN is owning the tunnel.");
                DiagnosticLog.Write("AndroidVpnService", "Builder.Establish() returned a TUN file descriptor");

                int tunFd = tunInterface.DetachFd();
                tunInterface.Dispose();

                string? bridgeError = XRayCoreWrapper.StartAndroidTunnel(
                    tunFd,
                    proxyPort,
                    udpEnabled,
                    localProxyCredentials);
                if (!string.IsNullOrWhiteSpace(bridgeError))
                {
                    XRayCoreWrapper.StopAndroidTunnel();
                    throw new InvalidOperationException(bridgeError);
                }

                EnsureHealthTimer();
            }

            DiagnosticLog.Write(
                "AndroidVpnService",
                $"Android VPN established with proxyPort={proxyPort}, tunAddress={tunAddress}, dns={dns}, udpEnabled={udpEnabled}, authEnabled={localProxyCredentials.HasValue}, appRulesMode={appRulesMode}, appPackages={string.Join(",", appPackages)}");
        }

        private void StartForegroundCompat()
        {
            Notification notification = AndroidConnectionNotificationManager.BuildForegroundNotification(this);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(
                    AndroidConnectionNotificationManager.ForegroundNotificationId,
                    notification,
                    ForegroundService.TypeSpecialUse);
            }
            else
            {
                StartForeground(AndroidConnectionNotificationManager.ForegroundNotificationId, notification);
            }
        }

        private void EnsureHealthTimer()
        {
            healthTimer?.Dispose();
            healthTimer = new Timer(
                callback: static state =>
                {
                    if (state is not AndroidVpnService service)
                        return;

                    if (XRayCoreWrapper.IsAndroidTunnelRunning())
                        return;

                    string message = XRayCoreWrapper.GetAndroidTunnelLastError()
                        ?? "Android tunnel bridge stopped unexpectedly.";
                    DiagnosticLog.Write("AndroidVpnService", message);
                    service.StopVpn(message);
                    service.StopSelf();
                },
                state: this,
                dueTime: TimeSpan.FromSeconds(2),
                period: TimeSpan.FromSeconds(2));
        }

        private void StopVpn(string reason)
        {
            lock (SyncRoot)
            {
                StopVpnCore(reason);
            }
        }

        private void ResetVpnCore(string reason)
        {
            healthTimer?.Dispose();
            healthTimer = null;

            try
            {
                XRayCoreWrapper.StopAndroidTunnel();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.ResetTunnel", ex);
            }
        }

        private void StopVpnCore(string reason)
        {
            healthTimer?.Dispose();
            healthTimer = null;

            try
            {
                XRayCoreWrapper.StopAndroidTunnel();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.StopTunnel", ex);
            }

            AndroidVpnServiceController.NotifyStopped(reason);

            try
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.StopForeground", ex);
            }

            AndroidConnectionNotificationManager.MarkStopped();
        }

        private static string[] SplitDnsServers(string dns)
        {
            string[] servers = dns
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(server => server.Trim())
                .Where(server => !string.IsNullOrWhiteSpace(server))
                .ToArray();

            return servers.Length == 0 ? new[] { "8.8.8.8" } : servers;
        }

        private void ApplyApplicationRules(Builder builder, AppRulesMode mode, IEnumerable<string> packages)
        {
            string[] packageArray = packages as string[] ?? packages.ToArray();
            DiagnosticLog.Write($"[AppRules] ApplyApplicationRules: mode={mode}, packageCount={packageArray.Length}");

            if (mode == AppRulesMode.ONLY_SELECTED_APPS)
            {
                if (packageArray.Length == 0)
                {
                    // Whitelist with zero entries would silently route ALL apps through the VPN
                    // because Android falls back to "no allow-list" when no allowed package is
                    // registered. The user explicitly opted for "only selected apps", so refuse
                    // to start instead of producing surprising routing.
                    throw new InvalidOperationException(
                        "Whitelist mode is enabled but no apps are selected. "
                        + "Open Settings → App rules → Manage and choose at least one application, "
                        + "or switch the mode back to \"All apps\".");
                }

                DiagnosticLog.Write("[AppRules] → TryAllowUserSelectedApplications (AddAllowedApplication)");
                int allowed = TryAllowUserSelectedApplications(builder, packageArray);

                if (allowed == 0)
                {
                    // Every requested package failed to register (e.g. all of them were uninstalled
                    // after the template was saved). Without at least one allowed app the VPN would
                    // route everything; fail closed with a clear message instead.
                    throw new InvalidOperationException(
                        "Whitelist mode is enabled but none of the selected apps are installed on this device. "
                        + "Open Settings → App rules → Manage and re-pick the applications you want to route through the VPN.");
                }

                return;
            }

            DiagnosticLog.Write("[AppRules] → TryExcludeOwnProcess + TryExcludeUserSelectedApplications (AddDisallowedApplication)");
            TryExcludeOwnProcess(builder);
            TryExcludeUserSelectedApplications(builder, packageArray);
        }

        private void TryExcludeOwnProcess(Builder builder)
        {
            try
            {
                builder.AddDisallowedApplication(PackageName!);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.AddDisallowedApplication", ex);
            }
        }

        private void TryExcludeUserSelectedApplications(Builder builder, IEnumerable<string> packages)
        {
            int added = 0;
            foreach (string packageName in packages)
            {
                try
                {
                    if (string.Equals(packageName, PackageName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    builder.AddDisallowedApplication(packageName);
                    added++;
                    DiagnosticLog.Write($"[AppRules] Disallowed: {packageName}");
                }
                catch (PackageManager.NameNotFoundException ex)
                {
                    DiagnosticLog.WriteException($"AndroidVpnService.AddDisallowedApplication.{packageName}", ex);
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException($"AndroidVpnService.AddDisallowedApplication.{packageName}", ex);
                }
            }

            DiagnosticLog.Write($"[AppRules] Total disallowed: {added}");
        }

        private int TryAllowUserSelectedApplications(Builder builder, IEnumerable<string> packages)
        {
            int added = 0;
            int skippedSelf = 0;
            int skippedMissing = 0;

            foreach (string packageName in packages)
            {
                try
                {
                    if (string.Equals(packageName, PackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        skippedSelf++;
                        DiagnosticLog.Write($"[AppRules] Skipped own package in whitelist: {packageName}");
                        continue;
                    }

                    builder.AddAllowedApplication(packageName);
                    added++;
                    DiagnosticLog.Write($"[AppRules] Allowed: {packageName}");
                }
                catch (PackageManager.NameNotFoundException ex)
                {
                    skippedMissing++;
                    DiagnosticLog.WriteException($"AndroidVpnService.AddAllowedApplication.{packageName}", ex);
                }
                catch (Exception ex)
                {
                    skippedMissing++;
                    DiagnosticLog.WriteException($"AndroidVpnService.AddAllowedApplication.{packageName}", ex);
                }
            }

            DiagnosticLog.Write($"[AppRules] Total allowed: {added}, skippedSelf={skippedSelf}, skippedMissing={skippedMissing}");
            return added;
        }

        private void TryEnableIpv6(Builder builder)
        {
            try
            {
                builder.AddAddress(DefaultIpv6Address, DefaultIpv6PrefixLength);
                builder.AddRoute("::", 0);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidVpnService.EnableIpv6", ex);
            }
        }

        private static AppRulesMode NormalizeAppRulesMode(int rawValue)
        {
            return rawValue switch
            {
                (int)AppRulesMode.BYPASS_SELECTED_APPS => AppRulesMode.BYPASS_SELECTED_APPS,
                (int)AppRulesMode.ONLY_SELECTED_APPS => AppRulesMode.ONLY_SELECTED_APPS,
                _ => AppRulesMode.ALL_APPS
            };
        }
    }
}
