using System.Threading;

namespace InvisibleManXRay.Core
{
    using Models;

    public class InvisibleManXRayCore
    {
        public static InvisibleManXRayCore Instance;

        public void Initialize()
        {
            if (Instance != null)
                return;
            
            Instance = this;
        }

        public void Run(Config config)
        {
            Thread xrayCoreThread = new Thread(() => {
                XRayCoreWrapper.Run(config.Path);
            });
            
            xrayCoreThread.Start();
        }
    }
}