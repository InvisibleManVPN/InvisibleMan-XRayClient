using System;
using System.Diagnostics;
using System.Linq;

namespace InvisibleGorillaXRay.Mac.Handlers.Proxies
{
    using InvisibleGorillaXRay.Handlers.Proxies;
    using InvisibleGorillaXRay.Models;

    public class MacProxy : IProxy
    {
        private string cachedService;
        private bool isCancelled;

        public Status Enable(string address, int port)
        {
            isCancelled = false;
            try
            {
                string service = GetActiveNetworkService();
                if (string.IsNullOrEmpty(service))
                    return new Status(Code.ERROR, SubCode.CANT_PROXY, "No active network service found.");

                if (isCancelled) return new Status(Code.ERROR, SubCode.CANCELED, null);

                RunNetworkSetup($"-setwebproxy \"{service}\" {address} {port}");
                RunNetworkSetup($"-setsecurewebproxy \"{service}\" {address} {port}");
                RunNetworkSetup($"-setproxybypassdomains \"{service}\" localhost 127.0.0.1");

                return new Status(Code.SUCCESS, SubCode.SUCCESS, null);
            }
            catch (Exception ex)
            {
                return new Status(Code.ERROR, SubCode.CANT_PROXY, ex.Message);
            }
        }

        public void Disable()
        {
            try
            {
                string service = cachedService ?? GetActiveNetworkService();
                if (string.IsNullOrEmpty(service)) return;

                RunNetworkSetup($"-setwebproxystate \"{service}\" off");
                RunNetworkSetup($"-setsecurewebproxystate \"{service}\" off");
            }
            catch { }
        }

        public void Cancel()
        {
            isCancelled = true;
            Disable();
        }

        private string GetActiveNetworkService()
        {
            if (!string.IsNullOrEmpty(cachedService)) return cachedService;

            string output = RunNetworkSetup("-listallhardwareports");
            string[] lines = output.Split('\n');

            string currentService = null;
            foreach (string line in lines)
            {
                if (line.StartsWith("Hardware Port:"))
                    currentService = line.Substring("Hardware Port:".Length).Trim();

                if (line.StartsWith("Device:") && currentService != null)
                {
                    string info = RunNetworkSetup($"-getinfo \"{currentService}\"");
                    if (info.Contains("IP address") && !info.Contains("IP address: none"))
                    {
                        cachedService = currentService;
                        return cachedService;
                    }
                }
            }

            string[] fallbackServices = { "Wi-Fi", "Ethernet", "USB 10/100/1000 LAN" };
            foreach (string svc in fallbackServices)
            {
                string info = RunNetworkSetup($"-getinfo \"{svc}\"");
                if (!info.Contains("** Error"))
                {
                    cachedService = svc;
                    return cachedService;
                }
            }

            return null;
        }

        private static string RunNetworkSetup(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/networksetup",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
