using System;
using System.IO;

namespace InvisibleGorillaXRay.Linux.Handlers.Settings
{
    using InvisibleGorillaXRay.Handlers.Settings.Startup;

    /// <summary>
    /// XDG autostart implementation: writes ~/.config/autostart/invisible-gorilla-xray.desktop.
    /// Honoured by GNOME, KDE, XFCE, MATE, Cinnamon and most XDG-compliant sessions.
    /// </summary>
    public class LinuxStartup : IStartupSetting
    {
        private static readonly string AutostartDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "autostart");

        private static readonly string DesktopPath = Path.Combine(
            AutostartDir, "invisible-gorilla-xray.desktop");

        public void EnableRunAtStartup()
        {
            try
            {
                if (!Directory.Exists(AutostartDir))
                    Directory.CreateDirectory(AutostartDir);

                string executablePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? string.Empty;

                string contents =
                    "[Desktop Entry]\n" +
                    "Type=Application\n" +
                    "Name=Invisible Gorilla XRay\n" +
                    "Comment=Secure local proxy / TUN client\n" +
                    $"Exec={EscapeExec(executablePath)}\n" +
                    "Icon=invisible-gorilla-xray\n" +
                    "Terminal=false\n" +
                    "X-GNOME-Autostart-enabled=true\n" +
                    "Categories=Network;\n";

                File.WriteAllText(DesktopPath, contents);
            }
            catch { }
        }

        public void DisableRunAtStartup()
        {
            try
            {
                if (File.Exists(DesktopPath))
                    File.Delete(DesktopPath);
            }
            catch { }
        }

        private static string EscapeExec(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.Contains(' ') ? $"\"{path}\"" : path;
        }
    }
}
