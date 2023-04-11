using Microsoft.Win32;

namespace InvisibleManXRay.Handlers.Proxies
{
    using Models;
    using Values;

    public class WindowsProxy : IProxy
    {
        private const string INTERNET_SETTINGS = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const string PROXY_ENABLE = "ProxyEnable";
        private const string PROXY_SERVER = "ProxyServer";

        public Status Enable(string ip, int port)
        {
            RegistryKey registry = GetInternetSettingsRegistry();

            try
            {
                registry.SetValue(PROXY_ENABLE, 1);
                registry.SetValue(PROXY_SERVER, $"{ip}:{port}");
                
                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }
            catch
            {
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.CANT_PROXY,
                    content: Message.CANT_PROXY_SYSTEM
                );
            }
        }

        public void Disable()
        {
            RegistryKey registry = GetInternetSettingsRegistry();
            registry.SetValue(PROXY_ENABLE, 0);
        }

        private RegistryKey GetInternetSettingsRegistry()
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey(INTERNET_SETTINGS, true);
            if (registry == null)
                registry = Registry.CurrentUser.CreateSubKey(INTERNET_SETTINGS);
            
            return registry;
        }
    }
}