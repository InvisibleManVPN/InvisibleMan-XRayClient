using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Content.PM;

namespace InvisibleGorillaXRay.Android.Services
{
    using InvisibleGorillaXRay.Core;

    internal sealed class AndroidInstalledAppInfo
    {
        public string PackageName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string IconRef { get; init; } = string.Empty;
        public bool IsSystemApp { get; init; }
    }

    internal static class AndroidInstalledAppDiscovery
    {
        public static IReadOnlyList<AndroidInstalledAppInfo> GetLaunchableApps()
        {
            try
            {
                Context? context = global::Android.App.Application.Context;
                if (context == null)
                    return Array.Empty<AndroidInstalledAppInfo>();

                PackageManager? packageManager = context.PackageManager;
                if (packageManager == null)
                    return Array.Empty<AndroidInstalledAppInfo>();

                Intent intent = new Intent(Intent.ActionMain);
                intent.AddCategory(Intent.CategoryLauncher);

                IList<ResolveInfo>? activities = null;
                try
                {
                    activities = packageManager.QueryIntentActivities(intent, 0);
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException("AndroidInstalledAppDiscovery.QueryIntentActivities", ex);
                    return Array.Empty<AndroidInstalledAppInfo>();
                }

                if (activities == null || activities.Count == 0)
                    return Array.Empty<AndroidInstalledAppInfo>();

                Dictionary<string, AndroidInstalledAppInfo> apps = new(StringComparer.OrdinalIgnoreCase);

                foreach (ResolveInfo? activity in activities)
                {
                    try
                    {
                        if (activity == null)
                            continue;

                        string packageName = activity.ActivityInfo?.PackageName?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(packageName))
                            continue;

                        if (string.Equals(packageName, context.PackageName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        ApplicationInfo? applicationInfo = activity.ActivityInfo?.ApplicationInfo;
                        bool isSystemApp = applicationInfo != null
                            && (applicationInfo.Flags & ApplicationInfoFlags.System) == ApplicationInfoFlags.System;

                        string displayName =
                            activity.LoadLabel(packageManager)?.ToString()?.Trim()
                            ?? applicationInfo?.LoadLabel(packageManager)?.ToString()?.Trim()
                            ?? packageName;

                        if (apps.TryGetValue(packageName, out AndroidInstalledAppInfo? existing))
                        {
                            if (!existing.IsSystemApp && isSystemApp)
                                continue;
                        }

                        apps[packageName] = new AndroidInstalledAppInfo
                        {
                            PackageName = packageName,
                            DisplayName = string.IsNullOrWhiteSpace(displayName) ? packageName : displayName,
                            IconRef = packageName,
                            IsSystemApp = isSystemApp
                        };
                    }
                    catch (Exception ex)
                    {
                        string packageName = activity?.ActivityInfo?.PackageName?.Trim() ?? "<unknown>";
                        DiagnosticLog.WriteException($"AndroidInstalledAppDiscovery.ResolveInfo.{packageName}", ex);
                    }
                }

                return apps.Values
                    .OrderBy(app => app.IsSystemApp)
                    .ThenBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(app => app.PackageName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidInstalledAppDiscovery.GetLaunchableApps", ex);
                return Array.Empty<AndroidInstalledAppInfo>();
            }
        }
    }
}
