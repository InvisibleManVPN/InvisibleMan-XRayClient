using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using InvisibleGorillaXRay.Core;

namespace InvisibleGorillaXRay.Mac;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                WriteStartupCrashLog("AppDomain.UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteStartupCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            WriteStartupCrashLog("Program.Main", ex);
            DiagnosticLog.WriteException("Program.Main", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    private static void WriteStartupCrashLog(string tag, Exception ex)
    {
        try
        {
            string logRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(logRoot))
                logRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support");

            string logDirectory = Path.Combine(logRoot, "InvisibleGorilla-XRay", "Logs");
            Directory.CreateDirectory(logDirectory);

            string logPath = Path.Combine(logDirectory, "startup-crash.log");
            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{tag}] {ex}{Environment.NewLine}");
        }
        catch
        {
            // Startup crash logging must not hide the original exception.
        }
    }
}
