using System;

namespace InvisibleGorillaXRay.Handlers
{
    using Models;
    using Tunnels;

    public class TunnelHandler : Handler
    {
        private ITunnel tunnel;
        private Func<ITunnel> tunnelFactory;

        public TunnelHandler(Func<ITunnel> tunnelFactory)
        {
            this.tunnelFactory = tunnelFactory;
        }

        public void Setup(
            Action onStartTunnelingService,
            Func<bool> isServiceRunning,
            Func<bool> isServicePortActive,
            Func<Status> connectTunnelingService,
            Func<string, Status> executeCommand
        )
        {
            this.tunnel = tunnelFactory();
            if (tunnel is WindowsTunnelBase wt)
            {
                wt.Setup(
                    onStartTunnelingService: onStartTunnelingService,
                    isServiceRunning: isServiceRunning,
                    isServicePortActive: isServicePortActive,
                    connectTunnelingService: connectTunnelingService,
                    executeCommand: executeCommand
                );
            }
        }

        public ITunnel GetTunnel() => tunnel;
    }

    public abstract class WindowsTunnelBase : ITunnel
    {
        public abstract void Setup(
            Action onStartTunnelingService,
            Func<bool> isServiceRunning,
            Func<bool> isServicePortActive,
            Func<Status> connectTunnelingService,
            Func<string, Status> executeCommand
        );

        public abstract Status Enable(string ip, int port, string address, string server, string dns, LocalProxyCredentials localProxyCredentials);
        public abstract void Disable();
        public abstract void Cancel();
    }
}
