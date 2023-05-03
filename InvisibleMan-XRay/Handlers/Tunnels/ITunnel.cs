namespace InvisibleManXRay.Handlers.Tunnels
{
    using Models;

    public interface ITunnel
    {
        Status Enable(string ip, int port, string address, string server, string dns);
        void Disable();
        void Cancel();
    }
}