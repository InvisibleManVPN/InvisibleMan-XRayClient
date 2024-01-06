using System;
using System.Net;

namespace InvisibleManXRay.Handlers.Tunnels
{
    using Foundation;
    using Models;
    using Values;

    public class WindowsTunnel : ITunnel
    {
        private bool isCanceled;
        private Scheduler scheduler;

        private Action onStartTunnelingService;
        private Func<bool> isServiceRunning;
        private Func<bool> isServicePortActive;
        private Func<Status> connectTunnelingService;
        private Func<string, Status> executeCommand;

        private const string NETWORK_INTERFACE_NAME = "InvisibleMan-XRay";

        public WindowsTunnel()
        {
            this.scheduler = new Scheduler();
        }

        public void Setup(
            Action onStartTunnelingService,
            Func<bool> isServiceRunning,
            Func<bool> isServicePortActive,
            Func<Status> connectTunnelingService,
            Func<string, Status> executeCommand
        )
        {
            this.onStartTunnelingService = onStartTunnelingService;
            this.isServiceRunning = isServiceRunning;
            this.isServicePortActive = isServicePortActive;
            this.connectTunnelingService = connectTunnelingService;
            this.executeCommand = executeCommand;
        }

        public Status Enable(string ip, int port, string address, string server, string dns)
        {
            try
            {
                FetchServerIP();
                StartTunnelingService();

                WaitUntilServiceWasRun(out bool isServiceRunConditionSatisfied);
                if (!isServiceRunConditionSatisfied)
                    return CancelStatus();
                
                WaitUntilServicePortWasActive(out bool isServicePortConditionSatisfied);
                if (!isServicePortConditionSatisfied)
                    return CancelStatus();
                
                Status connectingStatus = ConnectToTunnelingService();
                if (connectingStatus.Code == Code.ERROR)
                    return connectingStatus;

                Status enablingCommandStatus = ExecuteCommand(
                    command:
                        $"-command=enable " +
                        $"-device={NETWORK_INTERFACE_NAME} " +
                        $"-proxy={ip}:{port} " +
                        $"-address={address} " +
                        $"-server={server} " + 
                        $"-dns={dns}"
                );
                
                if(enablingCommandStatus.Code == Code.ERROR)
                    return enablingCommandStatus;
                
                if (isCanceled)
                    return CancelStatus();

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
                    content: Message.CANT_TUNNEL_SYSTEM
                );
            }

            void FetchServerIP()
            {
                Uri serverUri = new UriBuilder(server).Uri;
                server = Dns.GetHostAddresses(serverUri.Host)[0].ToString();
            }

            void StartTunnelingService() => onStartTunnelingService.Invoke();

            void WaitUntilServiceWasRun(out bool isConditionSatisfied)
            {
                scheduler.WaitUntil(
                    condition: IsServiceRunning,
                    cancellation: IsServiceCanceled,
                    isConditionSatisfied: out isConditionSatisfied
                );
            }

            void WaitUntilServicePortWasActive(out bool isConditionSatisfied)
            {
                scheduler.WaitUntil(
                    condition: IsServicePortActive,
                    cancellation: IsServiceCanceled,
                    isConditionSatisfied: out isConditionSatisfied
                );
            }

            bool IsServiceRunning() => isServiceRunning.Invoke();

            bool IsServicePortActive() => isServicePortActive.Invoke();

            bool IsServiceCanceled() => isCanceled;

            Status ConnectToTunnelingService() => connectTunnelingService.Invoke();
        }

        public void Disable()
        {
            isCanceled = false;
            ExecuteCommand(command: $"-command=disable");
        }

        public void Cancel()
        {
            isCanceled = true;
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

        private Status ExecuteCommand(string command) => executeCommand.Invoke(command);
    }
}