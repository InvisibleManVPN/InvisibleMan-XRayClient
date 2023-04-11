namespace InvisibleManXRay.Handlers.Proxies
{
    using Models;

    public interface IProxy
    {
        Status Enable(string ip, int port);
        void Disable();
    }
}