using System;

namespace InvisibleManXRay.Utilities
{
    public static class FileUtility
    {
        public static void SetTimeToNow(string path)
        {
            System.IO.File.SetCreationTime(path, DateTime.Now);
            System.IO.File.SetLastWriteTime(path, DateTime.Now);
        }

        public static string GetFullPath(string path)
        {
            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }
        }

        public static string GetDirectory(string path) => System.IO.Path.GetDirectoryName(path);

        public static string GetFileName(string path) => System.IO.Path.GetFileName(path);

        public static string GetFileUpdateTime(string path) => System.IO.File.GetLastWriteTime(path).ToShortDateString();

        public static bool IsFileExists(string path) => System.IO.File.Exists(path);

        public static bool IsDirectory(string path) => System.IO.Directory.Exists(path) && System.IO.File.GetAttributes(path).HasFlag(System.IO.FileAttributes.Directory);
    }
}