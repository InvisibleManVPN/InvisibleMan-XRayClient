using System;
using System.Text;
using System.Runtime.InteropServices;

namespace InvisibleGorillaXRay.Core
{
    using Models;
    using Values;

    internal class XRayCoreWrapper
    {
        public static string GetConfigFormat(string path)
        {
            IntPtr pathPtr = StringToUtf8Ptr(path);
            try
            {
                return Marshal.PtrToStringAnsi(GetConfigFormatNative(pathPtr));
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "GetConfigFormat")]
            static extern IntPtr GetConfigFormatNative(IntPtr pathPtr);
        }

        public static bool IsFileExists(string path)
        {
            IntPtr pathPtr = StringToUtf8Ptr(path);
            try
            {
                return IsFileExistsNative(pathPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(pathPtr);
            }

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "IsFileExists")]
            static extern bool IsFileExistsNative(IntPtr pathPtr);
        }

        public static string LoadConfig(string fileFormat, string filePath)
        {
            IntPtr formatPtr = StringToUtf8Ptr(fileFormat);
            IntPtr pathPtr = StringToUtf8Ptr(filePath);
            try
            {
                return Marshal.PtrToStringAnsi(LoadConfigNative(formatPtr, pathPtr));
            }
            finally
            {
                Marshal.FreeHGlobal(formatPtr);
                Marshal.FreeHGlobal(pathPtr);
            }

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "LoadConfig")]
            static extern IntPtr LoadConfigNative(IntPtr formatPtr, IntPtr pathPtr);
        }

        public static void StartServer(
            string config,
            int port,
            LogLevel logLevel,
            string logPath,
            bool isSocks,
            bool isUdpEnabled,
            LocalProxyCredentials? localProxyCredentials = null)
        {
            LocalProxyCredentials credentials = localProxyCredentials ?? LocalProxyCredentials.None;
            DiagnosticLog.Write("XRayWrapper", $"StartServer: port={port}, logLevel={logLevel}, logPath={logPath}, isSocks={isSocks}, isUdpEnabled={isUdpEnabled}, authEnabled={credentials.HasValue}");
            DiagnosticLog.Write("XRayWrapper", $"Config size: {config?.Length ?? 0} bytes");

            IntPtr logPathPtr = StringToUtf8Ptr(logPath);
            IntPtr usernamePtr = StringToUtf8Ptr(credentials.Username);
            IntPtr passwordPtr = StringToUtf8Ptr(credentials.Password);
            try
            {
                DiagnosticLog.Write("XRayWrapper", "Calling native StartServer...");
                StartServerNative(config, port, logLevel.ToString(), logPathPtr, isSocks, isUdpEnabled, usernamePtr, passwordPtr);
                DiagnosticLog.Write("XRayWrapper", "Native StartServer returned normally");
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("XRayWrapper.StartServer", ex);
                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(logPathPtr);
                Marshal.FreeHGlobal(usernamePtr);
                Marshal.FreeHGlobal(passwordPtr);
            }

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StartServer")]
            static extern void StartServerNative(string config, int port, string logLevel, IntPtr logPathPtr, bool isSocks, bool isUdpEnabled, IntPtr usernamePtr, IntPtr passwordPtr);
        }

        public static void StopServer()
        {
            StopServerNative();

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "StopServer")]
            static extern void StopServerNative();
        }

        public static int TestConnection(string config, int port)
        {
            return TestConnectionNative(config, port);

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "TestConnection")]
            static extern int TestConnectionNative(string config, int port);
        }

        public static string GetVersion()
        {
            return Marshal.PtrToStringAnsi(GetXRayCoreVersionNative());

            [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "GetXrayCoreVersion")]
            static extern IntPtr GetXRayCoreVersionNative();
        }

        private static IntPtr StringToUtf8Ptr(string str)
        {
            if (string.IsNullOrEmpty(str))
                str = string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr pointer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            Marshal.WriteByte(pointer, bytes.Length, 0);
            return pointer;
        }
    }
}
