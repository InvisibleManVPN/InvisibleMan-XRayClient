namespace InvisibleManXRay.Handlers.Proxies
{
    using Models;

    public interface IProxy
    {
        Status Enable(string address, int port);
        void Disable();
        void Cancel();
    }
}