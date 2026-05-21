using System;

namespace InvisibleGorillaXRay.Utilities
{
    public static class FileUtility
    {
        public static string GetValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return AllowJustAsciiCharacters(RemoveInvalidFileCharacters());

            string RemoveInvalidFileCharacters()
            {
                return string.Concat(fileName.Split(
                    System.IO.Path.GetInvalidFileNameChars()
                ));
            }

            string AllowJustAsciiCharacters(string name)
            {
                return System.Text.RegularExpressions.Regex.Replace(
                    input: name, 
                    pattern: @"[^\u0000-\u007F]+", 
                    replacement: string.Empty
                ).Trim();
            }
        }

        public static void SetFileTimeToNow(string path)
        {
            System.IO.File.SetCreationTime(path, DateTime.Now);
            System.IO.File.SetLastWriteTime(path, DateTime.Now);
        }

        public static void SetDirectoryTimeToNow(string path)
        {
            System.IO.Directory.SetCreationTime(path, DateTime.Now);
            System.IO.Directory.SetLastWriteTime(path, DateTime.Now);
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

        public static void TryDeleteDirectory(string path)
        {
            try
            {
                if (System.IO.Directory.Exists(path))
                    System.IO.Directory.Delete(path);
            }
            catch
            {
            }
        }

        public static void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(path);

        public static string GetDirectory(string path) => System.IO.Path.GetDirectoryName(path);

        public static string GetFileName(string path) => System.IO.Path.GetFileName(path);

        public static string GetFileUpdateTime(string path) => System.IO.File.GetLastWriteTime(path).ToShortDateString();

        public static bool IsFileExists(string path) => !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);

        public static bool IsDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                return System.IO.Directory.Exists(path) 
                    && System.IO.File.GetAttributes(path).HasFlag(System.IO.FileAttributes.Directory);
            }
            catch
            {
                return false;
            }
        }
    }
}
