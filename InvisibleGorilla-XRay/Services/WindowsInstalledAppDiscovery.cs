using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace InvisibleGorillaXRay.Services
{
    public sealed class WindowsInstalledAppInfo
    {
        public string ExecutablePath { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string IconRef { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
    }

    public static class WindowsInstalledAppDiscovery
    {
        private static readonly string[] UninstallRoots =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        public static IReadOnlyList<WindowsInstalledAppInfo> GetApps()
        {
            Dictionary<string, WindowsInstalledAppInfo> apps = new(StringComparer.OrdinalIgnoreCase);

            LoadRegistryApps(RegistryHive.LocalMachine, RegistryView.Registry64);
            LoadRegistryApps(RegistryHive.LocalMachine, RegistryView.Registry32);
            LoadRegistryApps(RegistryHive.CurrentUser, RegistryView.Registry64);
            LoadRegistryApps(RegistryHive.CurrentUser, RegistryView.Registry32);
            LoadRunningProcesses();

            return apps.Values
                .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(app => app.ExecutablePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            void LoadRegistryApps(RegistryHive hive, RegistryView view)
            {
                try
                {
                    using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
                    foreach (string uninstallRoot in UninstallRoots)
                    {
                        using RegistryKey? uninstallKey = baseKey.OpenSubKey(uninstallRoot);
                        if (uninstallKey == null)
                            continue;

                        foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                        {
                            using RegistryKey? appKey = uninstallKey.OpenSubKey(subKeyName);
                            if (appKey == null)
                                continue;

                            string displayName = appKey.GetValue("DisplayName")?.ToString()?.Trim() ?? string.Empty;
                            string rawIcon = appKey.GetValue("DisplayIcon")?.ToString()?.Trim() ?? string.Empty;
                            string executablePath = NormalizeExecutablePath(rawIcon);
                            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(executablePath))
                                continue;

                            AddApp(executablePath, displayName, rawIcon, $"registry:{hive}:{view}");
                        }
                    }
                }
                catch
                {
                }
            }

            void LoadRunningProcesses()
            {
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        string executablePath = NormalizeExecutablePath(process.MainModule?.FileName);
                        if (string.IsNullOrWhiteSpace(executablePath))
                            continue;

                        string displayName = !string.IsNullOrWhiteSpace(process.MainWindowTitle)
                            ? process.MainWindowTitle.Trim()
                            : Path.GetFileNameWithoutExtension(executablePath);

                        AddApp(executablePath, displayName, executablePath, "process");
                    }
                    catch
                    {
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }

            void AddApp(string executablePath, string displayName, string iconRef, string source)
            {
                if (apps.ContainsKey(executablePath))
                    return;

                apps[executablePath] = new WindowsInstalledAppInfo
                {
                    ExecutablePath = executablePath,
                    DisplayName = string.IsNullOrWhiteSpace(displayName)
                        ? Path.GetFileNameWithoutExtension(executablePath)
                        : displayName,
                    IconRef = string.IsNullOrWhiteSpace(iconRef) ? executablePath : iconRef,
                    Source = source
                };
            }
        }

        private static string NormalizeExecutablePath(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return string.Empty;

            string normalized = rawPath.Trim().Trim('"');
            int executableIndex = normalized.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (executableIndex < 0)
                return string.Empty;

            normalized = normalized.Substring(0, executableIndex + 4).Trim().Trim('"');
            if (normalized.IndexOfAny(new[] { '*', '?' }) >= 0)
                return string.Empty;

            try
            {
                return File.Exists(normalized)
                    ? Path.GetFullPath(normalized)
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
