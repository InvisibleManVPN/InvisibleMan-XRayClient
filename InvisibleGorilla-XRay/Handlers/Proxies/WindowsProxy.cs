using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace InvisibleGorillaXRay.Handlers.Proxies
{
    using Core;
    using Services;
    using Models;
    using Values;

    public class WindowsProxy : IProxy
    {
        private bool isCanceled;

        private const string INTERNET_SETTINGS = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        // InternetSetOption constants
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;
        private const int INTERNET_OPTION_PER_CONNECTION_OPTION = 75;

        // Per-connection option IDs
        private const int INTERNET_PER_CONN_FLAGS = 1;
        private const int INTERNET_PER_CONN_PROXY_SERVER = 2;
        private const int INTERNET_PER_CONN_PROXY_BYPASS = 3;

        // Proxy type flags
        private const int PROXY_TYPE_DIRECT = 0x00000001;
        private const int PROXY_TYPE_PROXY = 0x00000002;

        private const string PROXY_BYPASS = "<local>;localhost;127.*;10.*;192.168.*";

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private const int WM_SETTINGCHANGE = 0x001A;

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InternetSetOption(
            IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SendNotifyMessage(
            IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct INTERNET_PER_CONN_OPTION_LIST
        {
            public int dwSize;
            public IntPtr pszConnection;
            public int dwOptionCount;
            public int dwOptionError;
            public IntPtr pOptions;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct INTERNET_PER_CONN_OPTION
        {
            public int dwOption;
            public INTERNET_PER_CONN_OPTION_VALUE Value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INTERNET_PER_CONN_OPTION_VALUE
        {
            [FieldOffset(0)] public int dwValue;
            [FieldOffset(0)] public IntPtr pszValue;
            [FieldOffset(0)] public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
        }

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public Status Enable(string address, int port)
        {
            DiagnosticLog.Write("WindowsProxy", $"Enable called: address={address}, port={port}");
            try
            {
                string proxyServer = $"{address}:{port}";

                // Set via per-connection options (updates the DefaultConnectionSettings blob)
                bool perConnResult = SetPerConnectionProxy(proxyServer, PROXY_BYPASS);
                DiagnosticLog.Write("WindowsProxy", $"SetPerConnectionProxy result: {perConnResult}");

                // Also set the legacy registry values for compatibility
                SetRegistryValues(proxyServer, PROXY_BYPASS);

                if (isCanceled)
                {
                    DiagnosticLog.Write("WindowsProxy", "Canceled during Enable");
                    return CancelStatus();
                }

                // Notify all applications
                NotifyProxyChanged();

                VerifyState();

                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("WindowsProxy.Enable", ex);
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.CANT_PROXY,
                    content: LocalizationService.GetTerm(Localization.CANT_PROXY_SYSTEM)
                );
            }
        }

        public void Disable()
        {
            DiagnosticLog.Write("WindowsProxy", "Disable called");
            isCanceled = false;
            try
            {
                // Set via per-connection options (updates the DefaultConnectionSettings blob)
                bool perConnResult = ClearPerConnectionProxy();
                DiagnosticLog.Write("WindowsProxy", $"ClearPerConnectionProxy result: {perConnResult}");

                // Also clear legacy registry values
                ClearRegistryValues();

                // Notify all applications
                NotifyProxyChanged();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("WindowsProxy.Disable", ex);
            }
        }

        public void Cancel()
        {
            isCanceled = true;
        }

        private bool SetPerConnectionProxy(string proxyServer, string proxyBypass)
        {
            var options = new INTERNET_PER_CONN_OPTION[3];

            // Option 1: Flags - enable direct + manual proxy
            options[0].dwOption = INTERNET_PER_CONN_FLAGS;
            options[0].Value.dwValue = PROXY_TYPE_DIRECT | PROXY_TYPE_PROXY;

            // Option 2: Proxy server address
            options[1].dwOption = INTERNET_PER_CONN_PROXY_SERVER;
            options[1].Value.pszValue = Marshal.StringToHGlobalAuto(proxyServer);

            // Option 3: Proxy bypass list
            options[2].dwOption = INTERNET_PER_CONN_PROXY_BYPASS;
            options[2].Value.pszValue = Marshal.StringToHGlobalAuto(proxyBypass);

            try
            {
                return ApplyPerConnectionOptions(options);
            }
            finally
            {
                Marshal.FreeHGlobal(options[1].Value.pszValue);
                Marshal.FreeHGlobal(options[2].Value.pszValue);
            }
        }

        private bool ClearPerConnectionProxy()
        {
            var options = new INTERNET_PER_CONN_OPTION[1];

            // Option 1: Flags - direct only (no proxy)
            options[0].dwOption = INTERNET_PER_CONN_FLAGS;
            options[0].Value.dwValue = PROXY_TYPE_DIRECT;

            return ApplyPerConnectionOptions(options);
        }

        private bool ApplyPerConnectionOptions(INTERNET_PER_CONN_OPTION[] options)
        {
            int optionSize = Marshal.SizeOf(typeof(INTERNET_PER_CONN_OPTION));
            IntPtr optionsPtr = Marshal.AllocHGlobal(optionSize * options.Length);

            try
            {
                for (int i = 0; i < options.Length; i++)
                {
                    IntPtr current = IntPtr.Add(optionsPtr, i * optionSize);
                    Marshal.StructureToPtr(options[i], current, false);
                }

                var optionList = new INTERNET_PER_CONN_OPTION_LIST
                {
                    dwSize = Marshal.SizeOf(typeof(INTERNET_PER_CONN_OPTION_LIST)),
                    pszConnection = IntPtr.Zero, // default (LAN) connection
                    dwOptionCount = options.Length,
                    dwOptionError = 0,
                    pOptions = optionsPtr
                };

                int listSize = optionList.dwSize;
                IntPtr listPtr = Marshal.AllocHGlobal(listSize);

                try
                {
                    Marshal.StructureToPtr(optionList, listPtr, false);
                    bool result = InternetSetOption(
                        IntPtr.Zero,
                        INTERNET_OPTION_PER_CONNECTION_OPTION,
                        listPtr,
                        listSize);

                    if (!result)
                    {
                        int error = Marshal.GetLastWin32Error();
                        DiagnosticLog.Write("WindowsProxy", $"InternetSetOption PER_CONNECTION failed, error={error}");
                    }

                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(listPtr);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(optionsPtr);
            }
        }

        private void SetRegistryValues(string proxyServer, string proxyBypass)
        {
            using (RegistryKey registry = GetInternetSettingsRegistry())
            {
                if (registry != null)
                {
                    registry.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    registry.SetValue("ProxyServer", proxyServer, RegistryValueKind.String);
                    registry.SetValue("ProxyOverride", proxyBypass, RegistryValueKind.String);
                    DiagnosticLog.Write("WindowsProxy", $"Registry set: ProxyEnable=1, ProxyServer={proxyServer}");
                }
            }
        }

        private void ClearRegistryValues()
        {
            using (RegistryKey registry = GetInternetSettingsRegistry())
            {
                if (registry != null)
                {
                    registry.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    registry.DeleteValue("ProxyOverride", false);
                    DiagnosticLog.Write("WindowsProxy", "Registry cleared: ProxyEnable=0");
                }
            }
        }

        private static void NotifyProxyChanged()
        {
            bool r1 = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            bool r2 = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            // Non-blocking broadcast: SendNotifyMessage returns immediately without waiting
            // for each window to process the message (prevents UI hang)
            SendNotifyMessage(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "internet");

            DiagnosticLog.Write("WindowsProxy", $"NotifyProxyChanged: SETTINGS_CHANGED={r1}, REFRESH={r2}, WM_SETTINGCHANGE sent");
        }

        private void VerifyState()
        {
            try
            {
                using (RegistryKey registry = Registry.CurrentUser.OpenSubKey(INTERNET_SETTINGS, false))
                {
                    if (registry != null)
                    {
                        object proxyEnable = registry.GetValue("ProxyEnable");
                        object proxyServer = registry.GetValue("ProxyServer");
                        DiagnosticLog.Write("WindowsProxy", $"VERIFY registry: ProxyEnable={proxyEnable}, ProxyServer={proxyServer}");
                    }
                }

                var connKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections", false);
                if (connKey != null)
                {
                    using (connKey)
                    {
                        byte[] dcs = connKey.GetValue("DefaultConnectionSettings") as byte[];
                        if (dcs != null && dcs.Length > 8)
                        {
                            int flags = dcs[8];
                            bool manualProxy = (flags & 0x02) != 0;
                            bool autoDetect = (flags & 0x08) != 0;
                            DiagnosticLog.Write("WindowsProxy", $"VERIFY blob: flags=0x{flags:X2}, manualProxy={manualProxy}, autoDetect={autoDetect}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("WindowsProxy.Verify", ex);
            }
        }

        private RegistryKey GetInternetSettingsRegistry()
        {
            return Registry.CurrentUser.OpenSubKey(INTERNET_SETTINGS, true)
                ?? Registry.CurrentUser.CreateSubKey(INTERNET_SETTINGS);
        }

        private Status CancelStatus()
        {
            isCanceled = false;
            return new Status(
                code: Code.INFO,
                subCode: SubCode.CANCELED,
                content: null
            );
        }
    }
}
