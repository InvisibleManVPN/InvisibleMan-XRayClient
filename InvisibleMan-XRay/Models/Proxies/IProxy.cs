namespace InvisibleManXRay.Models.Proxies
{
    public interface IProxy
    {
        void Enable(string ip, int port);
        void Disable();
    }
}