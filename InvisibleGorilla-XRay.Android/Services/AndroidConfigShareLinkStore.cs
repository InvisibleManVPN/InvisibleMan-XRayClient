using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace InvisibleGorillaXRay.Android.Services
{
    internal static class AndroidConfigShareLinkStore
    {
        private static string MetadataRoot => Path.Combine(
            InvisibleGorillaXRay.Values.Directory.ROOT,
            "Metadata",
            "ConfigLinks");

        public static void SaveSourceLink(string configPath, string sourceLink)
        {
            if (string.IsNullOrWhiteSpace(configPath) || string.IsNullOrWhiteSpace(sourceLink))
                return;

            Directory.CreateDirectory(MetadataRoot);
            File.WriteAllText(GetMetadataPath(configPath), sourceLink.Trim());
        }

        public static bool TryGetSourceLink(string configPath, out string? sourceLink)
        {
            sourceLink = null;
            if (string.IsNullOrWhiteSpace(configPath))
                return false;

            string metadataPath = GetMetadataPath(configPath);
            if (!File.Exists(metadataPath))
                return false;

            string value = File.ReadAllText(metadataPath).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            sourceLink = value;
            return true;
        }

        public static void DeleteSourceLink(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                return;

            string metadataPath = GetMetadataPath(configPath);
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
        }

        private static string GetMetadataPath(string configPath)
        {
            string normalizedPath = Path.GetFullPath(configPath);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
            return Path.Combine(MetadataRoot, $"{Convert.ToHexString(hash)}.txt");
        }
    }
}
