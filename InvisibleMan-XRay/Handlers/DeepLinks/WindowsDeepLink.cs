using System;
using System.Windows;
using Microsoft.Win32;

namespace InvisibleManXRay.Handlers.DeepLinks
{
    using Values;

    public class WindowsDeepLink : IDeepLink
    {
        private const string URI_SCHEME = $@"Software\Classes\{DeepLink.SCHEME}";
        private const string SHELL_OPEN_COMMAND = @"shell\open\command";
        private const string URL_PROTOCOL = "URL Protocol";

        private string AppName => Application.ResourceAssembly.GetName().Name;
        private string AppPath => Environment.ProcessPath;

        public void Register()
        {
            try
            {
                RegistryKey registry = GetApplicationClassRegistery();
                
                registry.SetValue("", $"URL:{AppName}");
                registry.SetValue(URL_PROTOCOL, "");
                registry.CreateSubKey(SHELL_OPEN_COMMAND).SetValue(
                    name: "",
                    value: $"\"{AppPath}\" \"%1\""
                );
            }
            catch
            {
            }
        }

        private RegistryKey GetApplicationClassRegistery()
        {
            RegistryKey registry = Registry.CurrentUser.CreateSubKey(URI_SCHEME);
            return registry;
        }
    }
}