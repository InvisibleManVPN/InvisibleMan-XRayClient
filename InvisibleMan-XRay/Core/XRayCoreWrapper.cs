using System;
using System.Runtime.InteropServices;

namespace InvisibleManXRay.Core
{
    using Values;

    internal class XRayCoreWrapper
    {
        public static string GetConfigFormat(string path)
        {
            return Marshal.PtrToStringAnsi(GetConfigFormat(path));

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "GetConfigFormat")]
            static extern IntPtr GetConfigFormat(string path);
        }

        public static bool IsFileExists(string path)
        {
            return IsFileExists(path);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "IsFileExists")]
            static extern bool IsFileExists(string path);
        }

        public static string LoadConfig(string fileFormat, string filePath)
        {
            return Marshal.PtrToStringAnsi(LoadConfig(fileFormat, filePath));

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "LoadConfig")]
            static extern IntPtr LoadConfig(string format, string file);
        }

        public static void StartServer(string config, int port)
        {
            StartServer(config, port);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StartServer")]
            static extern void StartServer(string config, int port);
        }
    }
}