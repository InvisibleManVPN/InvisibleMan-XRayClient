namespace InvisibleGorillaXRay.Handlers.Tunnels
{
    using Models;

    public interface ITunnel
    {
        Status Enable(string ip, int port, string address, string server, string dns, LocalProxyCredentials localProxyCredentials);
        void Disable();
        void Cancel();
    }
}