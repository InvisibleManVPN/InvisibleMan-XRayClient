namespace InvisibleManXRay.Services.Analytics.Configuration
{
    using Models.Analytics;

    public abstract class ConfigurationEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public ConfigurationEvent()
        {
            Scope = "Configuration";
            Params = new Param[] { };
        }
    }
}