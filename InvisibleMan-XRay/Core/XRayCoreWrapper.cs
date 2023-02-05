using System.Runtime.InteropServices;

namespace InvisibleManXRay.Core
{
    using Values;

    public class XRayCoreWrapper
    {
        [DllImport(Path.XRAY_CORE_DLL, EntryPoint = "Run")]
        public static extern void Run(string path);
    }
}