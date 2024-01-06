using System;

namespace InvisibleManXRay.Handlers
{
    using Processes;

    public class ProcessHandler : Handler
    {
        private TunnelProcess tunnelProcess;

        public TunnelProcess TunnelProcess => tunnelProcess;

        public void Setup(Func<int> getTunnelPort)
        {
            tunnelProcess = new TunnelProcess();
            tunnelProcess.Setup(getPort: getTunnelPort);
        }
    }
}