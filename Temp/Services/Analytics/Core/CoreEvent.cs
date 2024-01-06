namespace InvisibleManXRay.Services.Analytics.Core
{
    using Models.Analytics;

    public abstract class CoreEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public CoreEvent()
        {
            Scope = "Core";
            Params = new Param[] { };
        }
    }
}