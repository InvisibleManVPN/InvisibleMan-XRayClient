using System.Windows;

namespace InvisibleManXRay
{
    using Managers;

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppManager appManager = new AppManager();
            appManager.Initialize();
        }
    }
}
