using System;
using System.Net;
using System.Linq;
using System.Net.NetworkInformation;

namespace InvisibleManXRay.Handlers.Tunnels
{
    using Foundation;
    using Models;
    using Values;

    public class WindowsTunnel : ITunnel
    {
        private Processor processor;
        private Scheduler scheduler;

        private const string TUNNELING_PROCESS_NAME = "tunneling_process";
        private const string NETWORK_INTERFACE_NAME = "InvisibleMan-XRay";

        public WindowsTunnel()
        {
            this.processor = new Processor();
            this.scheduler = new Scheduler();
        }

        public Status Enable(string ip, int port, string address, string server, string dns)
        {
            try
            {
                FetchServerIP();
                StartTunnelingProcess();
                WaitUntilNetworkInterfaceCreated(out bool isConditionSatisfied);

                if (!isConditionSatisfied)
                    throw new Exception();

                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }
            catch
            {
                processor.StopProcessAndChildren(TUNNELING_PROCESS_NAME);

                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.CANT_TUNNEL,
                    content: Message.CANT_TUNNEL_SYSTEM
                );
            }

            void FetchServerIP()
            {
                Uri serverUri = new UriBuilder(server).Uri;
                server = Dns.GetHostAddresses(serverUri.Host)[0].ToString();
            }

            void StartTunnelingProcess()
            {
                processor.StartProcessAsThread(
                    processName: TUNNELING_PROCESS_NAME,
                    fileName: System.IO.Path.GetFullPath(Path.INVISIBLEMAN_TUN_EXE),
                    command: 
                        $"-device={NETWORK_INTERFACE_NAME} " +
                        $"-proxy={ip}:{port} " +
                        $"-address={address} " +
                        $"-server={server} " + 
                        $"-dns={dns}",
                    runAsAdmin: true
                );
            }

            void WaitUntilNetworkInterfaceCreated(out bool isConditionSatisfied)
            {
                scheduler.WaitUntil(
                    condition: IsInterfaceExists,
                    millisecondsTimeout: 6000,
                    isConditionSatisfied: out isConditionSatisfied
                );
            }

            bool IsInterfaceExists()
            {
                NetworkInterface networkInterface = FindNetworkInterface(NETWORK_INTERFACE_NAME);
                return networkInterface != null;
            }

            NetworkInterface FindNetworkInterface(string interfaceName)
            {
                return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                    ni => ni.Name.StartsWith(interfaceName) 
                );
            }
        }

        public void Disable()
        {
            processor.StopProcessAndChildren(TUNNELING_PROCESS_NAME);
        }
    }
}