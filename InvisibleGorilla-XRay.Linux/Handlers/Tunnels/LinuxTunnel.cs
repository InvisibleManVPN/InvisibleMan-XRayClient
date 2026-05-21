using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CorePath = InvisibleGorillaXRay.Values.Path;

namespace InvisibleGorillaXRay.Linux.Handlers.Tunnels
{
    using InvisibleGorillaXRay.Linux.Handlers.Settings;
    using InvisibleGorillaXRay.Handlers.Tunnels;
    using InvisibleGorillaXRay.Handlers;
    using InvisibleGorillaXRay.Models;

    /// <summary>
    /// Linux TUN implementation using xjasonlyu/tun2socks.
    /// Routes all traffic through a tun device to the local SOCKS5 listener.
    /// Requires CAP_NET_ADMIN — the build script ships a small pkexec-friendly
    /// invocation. Privileged commands are run via pkexec when available, with
    /// sudo as a fallback. Without privileges, returns an actionable error.
    /// </summary>
    public class LinuxTunnel : ITunnel
    {
        private Process? tun2socksProcess;
        private bool isCancelled;
        private string? originalGateway;
        private string? originalInterface;
        private const string TUN_DEVICE = "tun-igxray";

        public Status Enable(string ip, int port, string address, string server, string dns, LocalProxyCredentials localProxyCredentials)
        {
            isCancelled = false;

            if (!File.Exists(CorePath.TUN_EXE))
            {
                return new Status(
                    Code.ERROR,
                    SubCode.CANT_TUNNEL,
                    $"tun2socks binary not found at {CorePath.TUN_EXE}. Run ./build.sh to fetch and bundle it.");
            }

            string privileged = ResolvePrivilegedFront();
            if (string.IsNullOrEmpty(privileged))
            {
                return new Status(
                    Code.ERROR,
                    SubCode.CANT_TUNNEL,
                    "Neither pkexec nor sudo is available. Install policykit-1 (pkexec) for GNOME prompts, or sudo.");
            }

            try
            {
                Status appRulesStatus = PrepareAppRulesBridge(port, address, dns, localProxyCredentials);
                if (appRulesStatus.Code == Code.ERROR)
                    return appRulesStatus;

                SaveOriginalRoutes();

                CreateTunDevice(privileged, ip);

                StartTun2Socks(ip, port, localProxyCredentials);

                if (isCancelled)
                    return new Status(Code.INFO, SubCode.CANCELED, null);

                Thread.Sleep(1500);

                ConfigureRoutes(privileged, ip, server);

                if (!string.IsNullOrEmpty(dns))
                    ConfigureDns(privileged, dns);

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch (Exception ex)
            {
                Disable();
                return new Status(Code.ERROR, SubCode.CANT_TUNNEL, $"TUN error: {ex.Message}");
            }
        }

        public void Disable()
        {
            string privileged = ResolvePrivilegedFront();

            try { StopTun2Socks(); } catch { }
            try { RestoreRoutes(privileged); } catch { }
            try { RestoreDns(privileged); } catch { }
            try { DestroyTunDevice(privileged); } catch { }
            try { LinuxAppRulesBridge.Clear(); } catch { }
        }

        public void Cancel()
        {
            isCancelled = true;
            Disable();
        }

        private static Status PrepareAppRulesBridge(int socksPort, string tunnelAddress, string dns, LocalProxyCredentials localProxyCredentials)
        {
            SettingsHandler settingsHandler = new(() => new LinuxStartup());
            UserSettings settings = settingsHandler.UserSettings;
            return LinuxAppRulesBridge.Prepare(settings, socksPort, tunnelAddress, dns, localProxyCredentials);
        }

        private static string ResolvePrivilegedFront()
        {
            if (CommandExists("pkexec")) return "pkexec";
            if (CommandExists("sudo")) return "sudo --non-interactive";
            return string.Empty;
        }

        private void CreateTunDevice(string privileged, string ip)
        {
            string user = Environment.UserName;
            RunPrivileged(privileged, "ip", $"tuntap add dev {TUN_DEVICE} mode tun user {user}");
            RunPrivileged(privileged, "ip", $"addr add {ip}/24 dev {TUN_DEVICE}");
            RunPrivileged(privileged, "ip", $"link set dev {TUN_DEVICE} up");
        }

        private void DestroyTunDevice(string privileged)
        {
            if (string.IsNullOrEmpty(privileged)) return;
            RunPrivileged(privileged, "ip", $"link set dev {TUN_DEVICE} down");
            RunPrivileged(privileged, "ip", $"tuntap del dev {TUN_DEVICE} mode tun");
        }

        private void StartTun2Socks(string tunIp, int socksPort, LocalProxyCredentials localProxyCredentials)
        {
            string proxyArgument = localProxyCredentials?.HasValue == true
                ? localProxyCredentials.BuildSocks5Uri("127.0.0.1", socksPort)
                : $"socks5://127.0.0.1:{socksPort}";

            tun2socksProcess = new Process();
            tun2socksProcess.StartInfo = new ProcessStartInfo
            {
                FileName = CorePath.TUN_EXE,
                Arguments = $"-device {TUN_DEVICE} -proxy {proxyArgument} -interface lo",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            tun2socksProcess.Start();
        }

        private void StopTun2Socks()
        {
            if (tun2socksProcess != null && !tun2socksProcess.HasExited)
            {
                tun2socksProcess.Kill();
                tun2socksProcess.WaitForExit(3000);
                tun2socksProcess.Dispose();
                tun2socksProcess = null;
            }
        }

        private void SaveOriginalRoutes()
        {
            string output = RunCommandOutput("ip", "route show default");
            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("default")) continue;

                string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i] == "via") originalGateway = parts[i + 1];
                    if (parts[i] == "dev") originalInterface = parts[i + 1];
                }
                break;
            }
        }

        private void ConfigureRoutes(string privileged, string tunIp, string serverAddress)
        {
            if (!string.IsNullOrEmpty(originalGateway) && !string.IsNullOrEmpty(serverAddress))
                RunPrivileged(privileged, "ip", $"route add {serverAddress}/32 via {originalGateway}");

            RunPrivileged(privileged, "ip", $"route add 0.0.0.0/1 dev {TUN_DEVICE}");
            RunPrivileged(privileged, "ip", $"route add 128.0.0.0/1 dev {TUN_DEVICE}");
        }

        private void RestoreRoutes(string privileged)
        {
            if (string.IsNullOrEmpty(privileged)) return;
            RunPrivileged(privileged, "ip", "route del 0.0.0.0/1");
            RunPrivileged(privileged, "ip", "route del 128.0.0.0/1");
        }

        private void ConfigureDns(string privileged, string dns)
        {
            if (CommandExists("resolvectl"))
                RunPrivileged(privileged, "resolvectl", $"dns {TUN_DEVICE} {dns}");
        }

        private void RestoreDns(string privileged)
        {
            if (CommandExists("resolvectl"))
                RunPrivileged(privileged, "resolvectl", $"revert {TUN_DEVICE}");
        }

        private static bool CommandExists(string name)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "/usr/bin/env",
                    Arguments = $"which {name}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                })!;
                p.WaitForExit(2000);
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        private void RunPrivileged(string privileged, string command, string args)
        {
            string fullArgs = string.IsNullOrEmpty(privileged)
                ? args
                : $"{(privileged == "pkexec" ? "" : privileged.Substring("sudo ".Length))} {command} {args}".Trim();

            string fileName = privileged == "pkexec" ? "pkexec" : "sudo";
            string finalArgs = privileged == "pkexec"
                ? $"{command} {args}"
                : $"--non-interactive {command} {args}";

            try
            {
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = finalArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                p?.WaitForExit(8000);
            }
            catch { }
        }

        private string RunCommandOutput(string command, string args)
        {
            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                return output;
            }
            catch { return string.Empty; }
        }
    }
}
