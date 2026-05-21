using System;
using System.Diagnostics;

namespace InvisibleGorillaXRay.Linux.Handlers.Proxies
{
    using InvisibleGorillaXRay.Handlers.Proxies;
    using InvisibleGorillaXRay.Models;

    /// <summary>
    /// GNOME proxy controller via gsettings (org.gnome.system.proxy).
    /// Works on ALT Linux GNOME, Ubuntu GNOME, Fedora Workstation and any DE that
    /// honours the org.gnome.system.proxy schema (most GTK apps and Chromium).
    ///
    /// KDE / Wayland-only sessions without the GNOME schema are not covered here:
    /// users should fall back to TUN mode for system-wide coverage.
    /// </summary>
    public class LinuxProxy : IProxy
    {
        private bool isCancelled;
        private bool wasApplied;

        public Status Enable(string address, int port)
        {
            isCancelled = false;
            try
            {
                if (!CommandExists("gsettings"))
                {
                    return new Status(
                        Code.ERROR,
                        SubCode.CANT_PROXY,
                        "gsettings not found. System proxy requires GNOME or a DE exposing org.gnome.system.proxy. Use TUN mode instead.");
                }

                if (isCancelled) return new Status(Code.ERROR, SubCode.CANCELED, null);

                Run("gsettings", $"set org.gnome.system.proxy mode 'manual'");
                Run("gsettings", $"set org.gnome.system.proxy.http host '{address}'");
                Run("gsettings", $"set org.gnome.system.proxy.http port {port}");
                Run("gsettings", $"set org.gnome.system.proxy.https host '{address}'");
                Run("gsettings", $"set org.gnome.system.proxy.https port {port}");
                Run("gsettings", $"set org.gnome.system.proxy.socks host '{address}'");
                Run("gsettings", $"set org.gnome.system.proxy.socks port {port}");
                Run("gsettings",  "set org.gnome.system.proxy ignore-hosts \"['localhost', '127.0.0.0/8', '::1']\"");

                wasApplied = true;
                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch (Exception ex)
            {
                return new Status(Code.ERROR, SubCode.CANT_PROXY, ex.Message);
            }
        }

        public void Disable()
        {
            try
            {
                if (!wasApplied || !CommandExists("gsettings")) return;
                Run("gsettings", "set org.gnome.system.proxy mode 'none'");
                wasApplied = false;
            }
            catch { }
        }

        public void Cancel()
        {
            isCancelled = true;
            Disable();
        }

        private static bool CommandExists(string name)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/env",
                    Arguments = $"which {name}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi)!;
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(2000);
                return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch { return false; }
        }

        private static string Run(string command, string arguments)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            string result = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return result;
        }
    }
}
