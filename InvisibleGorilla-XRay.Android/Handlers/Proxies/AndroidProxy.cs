namespace InvisibleGorillaXRay.Android.Handlers.Proxies
{
    using InvisibleGorillaXRay.Core;
    using InvisibleGorillaXRay.Handlers.Proxies;
    using InvisibleGorillaXRay.Models;

    public sealed class AndroidProxy : IProxy
    {
        public Status Enable(string address, int port)
        {
            DiagnosticLog.Write(
                "AndroidProxy",
                $"Proxy mode requested. Android keeps Xray as a local listener at {address}:{port}.");

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: string.Empty
            );
        }

        public void Disable()
        {
            DiagnosticLog.Write("AndroidProxy", "Disable requested");
        }

        public void Cancel()
        {
            DiagnosticLog.Write("AndroidProxy", "Cancel requested");
        }
    }
}
