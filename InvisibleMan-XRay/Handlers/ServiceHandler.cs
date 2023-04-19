using System;

namespace InvisibleManXRay.Handlers
{
    using Services;

    public class ServiceHandler : Handler
    {
        private TunnelService tunnelService;

        public TunnelService TunnelService => tunnelService;

        public void Setup(Func<int> getTunnelPort)
        {
            tunnelService = new TunnelService();
            tunnelService.Setup(getPort: getTunnelPort);
        }
    }
}