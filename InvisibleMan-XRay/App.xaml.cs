using System.Windows;

namespace InvisibleManXRay
{
    using Core;

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            InvisibleManXRayCore core = new InvisibleManXRayCore();
            core.Initialize();
        }
    }
}
