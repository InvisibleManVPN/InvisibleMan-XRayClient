using System;
using System.IO;

namespace InvisibleGorillaXRay.Core
{
    internal static class DiagnosticLog
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "diagnostic.log");

        private static readonly object LockObj = new object();

        public static void Write(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                lock (LockObj)
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging should never crash the app
            }
        }

        public static void Write(string tag, string message)
        {
            Write($"[{tag}] {message}");
        }

        public static void WriteException(string tag, Exception ex)
        {
            Write($"[{tag}] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Write($"[{tag}] StackTrace: {ex.StackTrace}");
        }

        public static void Clear()
        {
            try
            {
                lock (LockObj)
                {
                    File.WriteAllText(LogFilePath, $"=== Diagnostic Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }
            }
            catch { }
        }
    }
}
