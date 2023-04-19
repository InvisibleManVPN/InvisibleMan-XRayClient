using System;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Tunnels;

    public class TunnelHandler : Handler
    {
        private ITunnel tunnel;

        public void Setup(
            Action onStartTunnelingService, 
            Func<bool> isServiceRunning,
            Func<bool> isServicePortActive,
            Func<Status> connectTunnelingService,
            Func<string, Status> executeCommand
        )
        {
            this.tunnel = LoadTunnel(
                onStartTunnelingService: onStartTunnelingService, 
                isServiceRunning: isServiceRunning, 
                isServicePortActive: isServicePortActive,
                connectTunnelingService: connectTunnelingService,
                executeCommand: executeCommand
            );
        }

        public ITunnel GetTunnel() => tunnel;

        private ITunnel LoadTunnel(
            Action onStartTunnelingService, 
            Func<bool> isServiceRunning,
            Func<bool> isServicePortActive,
            Func<Status> connectTunnelingService,
            Func<string, Status> executeCommand
        )
        {
            WindowsTunnel windowsTunnel = new WindowsTunnel();
            windowsTunnel.Setup(
                onStartTunnelingService: onStartTunnelingService, 
                isServiceRunning: isServiceRunning, 
                isServicePortActive: isServicePortActive,
                connectTunnelingService: connectTunnelingService,
                executeCommand: executeCommand
            );

            return windowsTunnel;
        }
    }
}