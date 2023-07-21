using System;
using System.Runtime.InteropServices;

namespace InvisibleManXRay.Core
{
    using Models;
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

        public static void StartServer(string config, int port, LogLevel logLevel, string logPath, bool isSocks, bool isUdpEnabled)
        {
            StartServer(config, port, logLevel.ToString(), logPath, isSocks, isUdpEnabled);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StartServer")]
            static extern void StartServer(string config, int port, string logLevel, string logPath, bool isSocks, bool isUdpEnabled);
        }

        public static void StopServer()
        {
            StopServer();

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StopServer")]
            static extern void StopServer();
        }

        public static int TestConnection(string config, int port)
        {
            return TestConnection(config, port);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "TestConnection")]
            static extern int TestConnection(string config, int port);
        }

        public static string GetVersion()
        {
            return Marshal.PtrToStringAnsi(GetXRayCoreVersion());

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "GetXrayCoreVersion")]
            static extern IntPtr GetXRayCoreVersion();
        }
    }
}