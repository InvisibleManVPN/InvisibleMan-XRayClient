using System;
using Microsoft.Win32;

namespace InvisibleManXRay.Handlers.Settings.Startup
{
    public class WindowsStartupSetting : IStartupSetting
    {
        private const string RUN = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string AppName = App.ResourceAssembly.GetName().Name;

        public void EnableRunAtStartup()
        {
            RegistryKey registry = GetRunRegistry();

            try
            {
                registry.SetValue(AppName, Environment.ProcessPath);
            }
            catch
            {

            }
        }

        public void DisableRunAtStartup()
        {
            RegistryKey registry = GetRunRegistry();
            
            try
            {
                registry.DeleteValue(AppName, false);
            }
            catch
            {

            }
        }

        private RegistryKey GetRunRegistry()
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey(RUN, true);
            if (registry == null)
                registry = Registry.CurrentUser.CreateSubKey(RUN);
            
            return registry;
        }
    }
}