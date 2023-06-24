namespace InvisibleManXRay.Handlers.Settings.Startup
{
    public interface IStartupSetting
    {
        void EnableRunAtStartup();
        void DisableRunAtStartup();
    }
}