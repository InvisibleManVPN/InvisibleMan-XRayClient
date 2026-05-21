namespace InvisibleGorillaXRay.Handlers.Settings.Startup
{
    public interface IStartupSetting
    {
        void EnableRunAtStartup();
        void DisableRunAtStartup();
    }
}