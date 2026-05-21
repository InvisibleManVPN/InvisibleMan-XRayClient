using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// NOTE: This class lives under "InvisibleGorillaXRay.Mac.Services" intentionally so
// that the AppRulesWindow / SettingsWindow XAML code-behind (which is reused from
// the Mac project via linked files) resolves to a working implementation here.
// The "Mac" naming is therefore only an internal artefact — at runtime it parses
// the standard XDG .desktop directories used by GNOME / KDE / XFCE.
namespace InvisibleGorillaXRay.Mac.Services
{
    public sealed class MacInstalledAppInfo
    {
        public string AppId { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string AppPath { get; init; } = string.Empty;
        public string IconRef { get; init; } = string.Empty;
    }

    public static class MacInstalledAppDiscovery
    {
        public static IReadOnlyList<MacInstalledAppInfo> GetApps()
        {
            Dictionary<string, MacInstalledAppInfo> apps = new(StringComparer.OrdinalIgnoreCase);

            foreach (string root in GetSearchRoots())
            {
                if (!Directory.Exists(root))
                    continue;

                IEnumerable<string> entries;
                try
                {
                    entries = Directory.EnumerateFiles(root, "*.desktop", SearchOption.TopDirectoryOnly);
                }
                catch { continue; }

                foreach (string file in entries)
                {
                    try
                    {
                        MacInstalledAppInfo? info = ReadDesktopEntry(file);
                        if (info == null) continue;
                        if (apps.ContainsKey(info.AppId)) continue;
                        apps[info.AppId] = info;
                    }
                    catch { }
                }
            }

            return apps.Values
                .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(app => app.AppId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetSearchRoots()
        {
            yield return "/usr/share/applications";
            yield return "/usr/local/share/applications";
            yield return "/var/lib/flatpak/exports/share/applications";

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".local", "share", "applications");
            yield return Path.Combine(home, ".local", "share", "flatpak", "exports", "share", "applications");
        }

        private static MacInstalledAppInfo? ReadDesktopEntry(string path)
        {
            string id = Path.GetFileNameWithoutExtension(path);
            string? name = null;
            string? exec = null;
            string? icon = null;
            bool noDisplay = false;
            string? type = null;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.TrimStart();
                if (line.Length == 0 || line.StartsWith('#')) continue;

                if (line.StartsWith("[") && !line.StartsWith("[Desktop Entry", StringComparison.Ordinal))
                {
                    // We only want the main [Desktop Entry] section.
                    if (name != null) break;
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "Name": name ??= value; break;
                    case "Exec": exec ??= value; break;
                    case "Icon": icon ??= value; break;
                    case "Type": type ??= value; break;
                    case "NoDisplay":
                    case "Hidden":
                        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                            noDisplay = true;
                        break;
                }
            }

            if (noDisplay) return null;
            if (!string.Equals(type, "Application", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.IsNullOrWhiteSpace(exec)) return null;

            return new MacInstalledAppInfo
            {
                AppId = id,
                DisplayName = string.IsNullOrWhiteSpace(name) ? id : name!,
                AppPath = path,
                IconRef = icon ?? string.Empty
            };
        }
    }
}
