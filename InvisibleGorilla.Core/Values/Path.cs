using System.IO;
using System.Runtime.InteropServices;

namespace InvisibleGorillaXRay.Values
{
    public static class Path
    {
        public static string USER_SETTINGS => System.IO.Path.Combine(Directory.ROOT, "Settings.json");
        public static string DIAGNOSTIC_LOG => System.IO.Path.Combine(Directory.ROOT, "diagnostic.log");

        public static string XRAY_CORE_LIB => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? System.IO.Path.Combine(Directory.LIBRARIES, "XRayCore.dll")
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? System.IO.Path.Combine(Directory.LIBRARIES, "XRayCore.dylib")
                : System.IO.Path.Combine(Directory.LIBRARIES, "libXRayCore.so");

        public static string INVISIBLEGORILLA_TUN_EXE => System.IO.Path.Combine(Directory.TUN, "InvisibleGorilla-TUN.exe");
        public static string INVISIBLEMAN_TUN_EXE => System.IO.Path.Combine(Directory.TUN, "InvisibleMan-TUN.exe");

        public static string TUN_EXE => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (File.Exists(INVISIBLEGORILLA_TUN_EXE) ? INVISIBLEGORILLA_TUN_EXE : INVISIBLEMAN_TUN_EXE)
            : System.IO.Path.Combine(Directory.TUN, "tun2socks");

        // Keep the old constant for backward compatibility with Windows project
        public static string XRAY_CORE_DLL => System.IO.Path.Combine(Directory.LIBRARIES, "XRayCore.dll");
    }
}
