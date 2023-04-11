namespace InvisibleManXRay.Handlers
{
    using Tunnels;

    public class TunnelHandler : Handler
    {
        private ITunnel tunnel;

        public TunnelHandler()
        {
            this.tunnel = LoadTunnel();
        }

        public ITunnel GetTunnel() => tunnel;

        private ITunnel LoadTunnel()
        {
            WindowsTunnel windowsTunnel = new WindowsTunnel();
            return windowsTunnel;
        }
    }
}