using System;
using System.IO;
using Android.App;

namespace InvisibleGorillaXRay.Android.Platforms
{
    using InvisibleGorillaXRay.Core;

    internal static class AndroidAppStorage
    {
        private static readonly string AppRoot = ResolveAppRoot();

        public static void ConfigureAppRoot()
        {
            InvisibleGorillaXRay.Values.Directory.SetRoot(AppRoot);
            InvisibleGorillaXRay.Values.Directory.EnsureWritableDirectories();
        }

        public static void EnsureRuntimeAssets()
        {
            ConfigureAppRoot();

            CopyAssetIfPresent("Runtime/geoip.dat", Path.Combine(InvisibleGorillaXRay.Values.Directory.ROOT, "geoip.dat"));
            CopyAssetIfPresent("Runtime/geosite.dat", Path.Combine(InvisibleGorillaXRay.Values.Directory.ROOT, "geosite.dat"));
            DeleteLegacyCopiedNativeRuntime();
        }

        private static bool CopyAssetIfPresent(string assetPath, string destinationPath, string? assetIdentity = null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? InvisibleGorillaXRay.Values.Directory.ROOT);

                using Stream assetStream = Application.Context.Assets!.Open(assetPath);
                string? identityPath = string.IsNullOrWhiteSpace(assetIdentity)
                    ? null
                    : destinationPath + ".asset-id";

                if (File.Exists(destinationPath))
                {
                    try
                    {
                        FileInfo destinationInfo = new FileInfo(destinationPath);
                        string currentIdentity = identityPath != null && File.Exists(identityPath)
                            ? File.ReadAllText(identityPath)
                            : string.Empty;

                        if (destinationInfo.Length == assetStream.Length &&
                            (identityPath == null || string.Equals(currentIdentity, assetIdentity, StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }

                using FileStream destinationStream = File.Create(destinationPath);
                assetStream.CopyTo(destinationStream);

                if (identityPath != null)
                    File.WriteAllText(identityPath, assetIdentity);

                return true;
            }
            catch
            {
                // Asset is optional during development; build scripts populate it for APK packaging.
                return false;
            }
        }

        private static string ResolveAppRoot()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                localAppData = AppContext.BaseDirectory;

            return Path.Combine(localAppData, "InvisibleGorilla-XRay");
        }

        private static void DeleteLegacyCopiedNativeRuntime()
        {
            try
            {
                string nativeLibPath = InvisibleGorillaXRay.Values.Path.XRAY_CORE_LIB;
                if (File.Exists(nativeLibPath))
                    File.Delete(nativeLibPath);

                string assetIdentityPath = nativeLibPath + ".asset-id";
                if (File.Exists(assetIdentityPath))
                    File.Delete(assetIdentityPath);
            }
            catch
            {
                // The packaged AndroidNativeLibrary is the source of truth.
            }
        }
    }
}
