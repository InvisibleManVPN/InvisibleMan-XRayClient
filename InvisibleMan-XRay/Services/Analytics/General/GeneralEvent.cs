namespace InvisibleManXRay.Services.Analytics.General
{
    using Models.Analytics;

    public abstract class GeneralEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public GeneralEvent()
        {
            Scope = "General";
            Params = new Param[] { };
        }
    }
}