using System;
using System.Text;
using System.Runtime.InteropServices;

namespace InvisibleManXRay.Core
{
    using Models;
    using Values;

    internal class XRayCoreWrapper
    {
        public static string GetConfigFormat(string path)
        {
            IntPtr pathPtr = StringToUtf8Ptr(path);
            return Marshal.PtrToStringAnsi(GetConfigFormat(pathPtr));

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "GetConfigFormat")]
            static extern IntPtr GetConfigFormat(IntPtr pathPtr);
        }

        public static bool IsFileExists(string path)
        {
            IntPtr pathPtr = StringToUtf8Ptr(path);
            return IsFileExists(pathPtr);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "IsFileExists")]
            static extern bool IsFileExists(IntPtr pathPtr);
        }

        public static string LoadConfig(string fileFormat, string filePath)
        {
            IntPtr formatPtr = StringToUtf8Ptr(fileFormat);
            IntPtr pathPtr = StringToUtf8Ptr(filePath);
            return Marshal.PtrToStringAnsi(LoadConfig(formatPtr, pathPtr));

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "LoadConfig")]
            static extern IntPtr LoadConfig(IntPtr formatPtr, IntPtr pathPtr);
        }

        public static void StartServer(string config, int port, LogLevel logLevel, string logPath, bool isSocks, bool isUdpEnabled)
        {
            StartServer(config, port, logLevel.ToString(), StringToUtf8Ptr(logPath), isSocks, isUdpEnabled);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StartServer")]
            static extern void StartServer(string config, int port, string logLevel, IntPtr logPathPtr, bool isSocks, bool isUdpEnabled);
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

        private static IntPtr StringToUtf8Ptr(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }
    }
}