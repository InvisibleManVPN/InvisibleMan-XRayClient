using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace InvisibleManXRay.Handlers.Processes
{
    using Foundation;
    using Services;
    using Models;
    using Utilities;
    using Values;

    public class TunnelProcess
    {
        private IPHostEntry hostEntry;
        private IPAddress address;
        private IPEndPoint endPoint;
        private Socket sender;

        private Func<int> getPort;
        private Processor processor;

        private const string INVISIBLEMAN_TUN_PROCESS = "InvisibleMan-TUN";

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public TunnelProcess()
        {
            this.hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            this.address = hostEntry.AddressList.First();

            this.processor = new Processor();
        }

        public void Setup(Func<int> getPort)
        {
            this.getPort = getPort;
        }

        public void Start()
        {
            if (IsProcessRunning())
                return;
            
            processor.StopSystemProcesses(INVISIBLEMAN_TUN_PROCESS);

            processor.StartProcess(
                processName: INVISIBLEMAN_TUN_PROCESS,
                fileName: System.IO.Path.GetFullPath(Path.INVISIBLEMAN_TUN_EXE),
                workingDirectory: Directory.TUN,
                command: $"-port={getPort.Invoke()}",
                runAsAdmin: true
            );
        }

        public Status Connect()
        {
            if (IsConnected())
                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );

            try
            {
                endPoint = new IPEndPoint(address, getPort.Invoke());
                sender = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(endPoint);
                System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

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
                    subCode: SubCode.CANT_CONNECT_TO_TUNNEL_SERVICE,
                    content: LocalizationService.GetTerm(Localization.CANT_CONNECT_TO_TUNNEL_SERVICE)
                );
            }

            bool IsConnected()
            {
                return sender != null && 
                    !((sender.Poll(1000, SelectMode.SelectRead) && (sender.Available == 0)) || !sender.Connected);
            }
        }

        public Status Execute(string command)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(command + "<EOF>");
                int bytesCount = sender.Send(bytes);

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
                    subCode: SubCode.CANT_TUNNEL,
                    content: LocalizationService.GetTerm(Localization.CANT_TUNNEL_SYSTEM)
                );
            }
        }

        public bool IsProcessRunning() => processor.IsProcessRunning(INVISIBLEMAN_TUN_PROCESS);

        public bool IsProcessPortActive() => NetworkUtility.IsPortActive(getPort.Invoke());
    }
}