using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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

                IEnumerable<string> appDirectories;
                try
                {
                    appDirectories = Directory.EnumerateDirectories(root, "*.app", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (string appDirectory in appDirectories)
                {
                    try
                    {
                        (string appId, string displayName) = ReadMetadata(appDirectory);
                        if (string.IsNullOrWhiteSpace(appId))
                            continue;

                        if (apps.ContainsKey(appId))
                            continue;

                        apps[appId] = new MacInstalledAppInfo
                        {
                            AppId = appId,
                            DisplayName = string.IsNullOrWhiteSpace(displayName)
                                ? Path.GetFileNameWithoutExtension(appDirectory)
                                : displayName,
                            AppPath = appDirectory,
                            IconRef = appDirectory
                        };
                    }
                    catch
                    {
                    }
                }
            }

            return apps.Values
                .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(app => app.AppId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetSearchRoots()
        {
            yield return "/Applications";
            yield return "/System/Applications";

            string userApplications = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Applications");
            yield return userApplications;
        }

        private static (string AppId, string DisplayName) ReadMetadata(string appDirectory)
        {
            string infoPlistPath = Path.Combine(appDirectory, "Contents", "Info.plist");
            if (!File.Exists(infoPlistPath))
            {
                string fallbackName = Path.GetFileNameWithoutExtension(appDirectory);
                return (appDirectory, fallbackName);
            }

            try
            {
                XDocument document = XDocument.Load(infoPlistPath);
                string? bundleIdentifier = FindPlistValue(document, "CFBundleIdentifier");
                string? displayName = FindPlistValue(document, "CFBundleDisplayName")
                    ?? FindPlistValue(document, "CFBundleName");

                if (!string.IsNullOrWhiteSpace(bundleIdentifier))
                    return (bundleIdentifier.Trim(), displayName?.Trim() ?? Path.GetFileNameWithoutExtension(appDirectory));
            }
            catch
            {
            }

            return (appDirectory, Path.GetFileNameWithoutExtension(appDirectory));
        }

        private static string? FindPlistValue(XDocument document, string keyName)
        {
            XElement? dict = document.Root?.Element("dict");
            if (dict == null)
                return null;

            List<XElement> elements = dict.Elements().ToList();
            for (int i = 0; i < elements.Count - 1; i++)
            {
                if (elements[i].Name.LocalName != "key")
                    continue;

                if (!string.Equals(elements[i].Value, keyName, StringComparison.Ordinal))
                    continue;

                return elements[i + 1].Value;
            }

            return null;
        }
    }
}
