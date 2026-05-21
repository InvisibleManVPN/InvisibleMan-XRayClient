using System;
using System.Diagnostics;
using System.IO;

namespace InvisibleGorillaXRay.Linux.Handlers.DeepLinks
{
    using InvisibleGorillaXRay.Handlers.DeepLinks;

    /// <summary>
    /// Registers config/share URI scheme associations through xdg-mime.
    /// Required pieces (the bundled .desktop file) are written by the build script;
    /// at runtime we just rebind the scheme handlers in case they were lost.
    /// </summary>
    public class LinuxDeepLink : IDeepLink
    {
        private static readonly string[] Schemes =
        {
            "x-scheme-handler/vless",
            "x-scheme-handler/vmess",
            "x-scheme-handler/invxray",
            "x-scheme-handler/ig-xray"
        };

        public void Register()
        {
            try
            {
                if (!CommandExists("xdg-mime")) return;
                if (!HasUserDesktopFile()) return;

                foreach (string scheme in Schemes)
                    Run("xdg-mime", $"default invisible-gorilla-xray.desktop {scheme}");
            }
            catch { }
        }

        private static bool HasUserDesktopFile()
        {
            string applications = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share", "applications", "invisible-gorilla-xray.desktop");
            return File.Exists(applications);
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

        private static void Run(string command, string args)
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            p?.WaitForExit(3000);
        }
    }
}
