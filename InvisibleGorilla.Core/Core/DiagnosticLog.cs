using System;
using System.IO;
using System.Text;

namespace InvisibleGorillaXRay.Core
{
    using Values;

    public static class DiagnosticLog
    {
        // 1 MB cap with simple rotation: when the active log grows past Threshold,
        // it is renamed to diagnostic.log.1 (replacing the previous .1 file) and a
        // fresh active log is started. This keeps roughly the last 1-2 MB of context
        // available for log sharing without ever growing without bound on device.
        private const long MaxActiveLogBytes = 1024L * 1024L;

        private static readonly object LockObj = new object();

        private static string LogFilePath => Values.Path.DIAGNOSTIC_LOG;
        private static string ArchivedLogFilePath => LogFilePath + ".1";

        public static string ActiveLogPath => LogFilePath;
        public static string ArchivedLogPath => ArchivedLogFilePath;

        public static void Write(string message)
        {
            try
            {
                Values.Directory.EnsureWritableDirectories();
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                lock (LockObj)
                {
                    RotateIfNeeded();
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

            Exception? inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 5)
            {
                Write($"[{tag}] InnerException[{depth}]: {inner.GetType().Name}: {inner.Message}");
                Write($"[{tag}] InnerStackTrace[{depth}]: {inner.StackTrace}");
                inner = inner.InnerException;
                depth++;
            }
        }

        public static void Clear()
        {
            try
            {
                Values.Directory.EnsureWritableDirectories();
                lock (LockObj)
                {
                    File.WriteAllText(LogFilePath, $"=== Diagnostic Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }
            }
            catch { }
        }

        public static void ClearAll()
        {
            try
            {
                Values.Directory.EnsureWritableDirectories();
                lock (LockObj)
                {
                    if (File.Exists(ArchivedLogFilePath))
                    {
                        try { File.Delete(ArchivedLogFilePath); } catch { }
                    }

                    File.WriteAllText(LogFilePath, $"=== Diagnostic Log Cleared: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }
            }
            catch { }
        }

        public static string ReadAll()
        {
            try
            {
                Values.Directory.EnsureWritableDirectories();
                lock (LockObj)
                {
                    StringBuilder builder = new StringBuilder();

                    if (File.Exists(ArchivedLogFilePath))
                    {
                        builder.Append(SafeReadAll(ArchivedLogFilePath));
                        if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
                            builder.Append(Environment.NewLine);
                    }

                    if (File.Exists(LogFilePath))
                    {
                        builder.Append(SafeReadAll(LogFilePath));
                    }

                    return builder.ToString();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeReadAll(string path)
        {
            try
            {
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                    return;

                FileInfo info = new FileInfo(LogFilePath);
                if (info.Length < MaxActiveLogBytes)
                    return;

                if (File.Exists(ArchivedLogFilePath))
                {
                    try { File.Delete(ArchivedLogFilePath); } catch { }
                }

                try { File.Move(LogFilePath, ArchivedLogFilePath); } catch { }
            }
            catch
            {
                // Best-effort rotation: never block normal logging on failure.
            }
        }
    }
}
