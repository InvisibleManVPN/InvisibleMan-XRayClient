using Microsoft.Win32;

namespace InvisibleManXRay.Handlers.Proxies
{
    public class WindowsProxy : IProxy
    {
        private const string INTERNET_SETTINGS = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const string PROXY_ENABLE = "ProxyEnable";
        private const string PROXY_SERVER = "ProxyServer";

        public void Enable(string ip, int port)
        {
            RegistryKey registry = GetInternetSettingsRegistry();

            registry.SetValue(PROXY_ENABLE, 1);
            registry.SetValue(PROXY_SERVER, $"{ip}:{port}");
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