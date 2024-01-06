namespace InvisibleManXRay.Services.Analytics.SettingsWindow
{
    using Models.Analytics;

    public abstract class SettingsWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public SettingsWindowEvent()
        {
            Scope = "SettingsWindow";
            Params = new Param[] { };
        }
    }
}