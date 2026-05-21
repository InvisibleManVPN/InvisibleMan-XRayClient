using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace InvisibleGorillaXRay.Handlers.Processes
{
    using Core;
    using Foundation;
    using Services;
    using Models;
    using Utilities;
    using Values;

    public class TunnelProcess
    {
        private static readonly string[] TunProcessNames = { "InvisibleGorilla-TUN", "InvisibleMan-TUN" };

        private IPEndPoint endPoint;
        private Socket sender;

        private Func<int> getPort;
        private Processor processor;
        private readonly string tunProcessName;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public TunnelProcess()
        {
            this.processor = new Processor();
            this.tunProcessName = System.IO.Path.GetFileNameWithoutExtension(Path.TUN_EXE);
        }

        public void Setup(Func<int> getPort)
        {
            this.getPort = getPort;
        }

        public void Start()
        {
            int port = getPort.Invoke();
            string fullTunExePath = System.IO.Path.GetFullPath(Path.TUN_EXE);
            string tunDirectoryFullPath = System.IO.Path.GetFullPath(Directory.TUN);

            DiagnosticLog.Write(
                "TunnelProcess",
                $"Start requested: processName={tunProcessName}, port={port}, exe={fullTunExePath}, cwd={tunDirectoryFullPath}");
            LogRuntimeFiles(tunDirectoryFullPath, fullTunExePath);

            if (IsProcessRunning())
            {
                DiagnosticLog.Write("TunnelProcess", "Start skipped because process is already running");
                return;
            }
            
            foreach (string processName in TunProcessNames)
            {
                DiagnosticLog.Write("TunnelProcess", $"Stopping old system process if present: {processName}");
                processor.StopSystemProcesses(processName);
            }

            processor.StartProcess(
                processName: tunProcessName,
                fileName: fullTunExePath,
                workingDirectory: Directory.TUN,
                command: $"-port={port}",
                runAsAdmin: true
            );

            DiagnosticLog.Write("TunnelProcess", $"StartProcess invoked for {tunProcessName}");
        }

        public Status Connect()
        {
            if (IsConnected())
            {
                DiagnosticLog.Write("TunnelProcess", "Connect skipped because socket is already connected");
                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }

            try
            {
                int port = getPort.Invoke();
                DiagnosticLog.Write(
                    "TunnelProcess",
                    $"Connect requested: endpoint=127.0.0.1:{port}, isProcessRunning={IsProcessRunning()}, isPortActive={IsProcessPortActive()}");

                endPoint = new IPEndPoint(IPAddress.Loopback, port);
                sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(endPoint);
                System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                DiagnosticLog.Write("TunnelProcess", "Socket connected to TUN service");

                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("TunnelProcess.Connect", ex);
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
                DiagnosticLog.Write("TunnelProcess", $"Execute requested: {command}");
                byte[] bytes = Encoding.ASCII.GetBytes(command + "<EOF>");
                int bytesCount = sender.Send(bytes);
                DiagnosticLog.Write("TunnelProcess", $"Execute sent {bytesCount} bytes");

                return new Status(
                    code: Code.SUCCESS,
                    subCode: SubCode.SUCCESS,
                    content: null
                );
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("TunnelProcess.Execute", ex);
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.CANT_TUNNEL,
                    content: LocalizationService.GetTerm(Localization.CANT_TUNNEL_SYSTEM)
                );
            }
        }

        public bool IsProcessRunning() => processor.IsProcessRunning(tunProcessName);

        public bool IsProcessPortActive() => NetworkUtility.IsPortActive(getPort.Invoke());

        private void LogRuntimeFiles(string tunDirectoryFullPath, string fullTunExePath)
        {
            string[] runtimeFiles = {
                fullTunExePath,
                System.IO.Path.Combine(tunDirectoryFullPath, "tun.dll"),
                System.IO.Path.Combine(tunDirectoryFullPath, "tun2socks.exe"),
                System.IO.Path.Combine(tunDirectoryFullPath, "wintun.dll")
            };

            foreach (string runtimeFile in runtimeFiles)
            {
                DiagnosticLog.Write(
                    "TunnelProcess",
                    $"Runtime file exists={System.IO.File.Exists(runtimeFile)} path={runtimeFile}");
            }
        }
    }
}