using System;
using System.IO;

namespace InvisibleGorillaXRay.Mac.Handlers.Settings
{
    using InvisibleGorillaXRay.Handlers.Settings.Startup;

    public class MacStartup : IStartupSetting
    {
        private static readonly string LaunchAgentsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents");

        private static readonly string PlistPath = Path.Combine(
            LaunchAgentsDir, "com.invisiblegorilla.xray.plist");

        public void EnableRunAtStartup()
        {
            try
            {
                if (!Directory.Exists(LaunchAgentsDir))
                    Directory.CreateDirectory(LaunchAgentsDir);

                string executablePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? string.Empty;

                string plistContent =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>com.invisiblegorilla.xray</string>
    <key>ProgramArguments</key>
    <array>
        <string>" + executablePath + @"</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>";

                File.WriteAllText(PlistPath, plistContent);
            }
            catch { }
        }

        public void DisableRunAtStartup()
        {
            try
            {
                if (File.Exists(PlistPath))
                    File.Delete(PlistPath);
            }
            catch { }
        }
    }
}
