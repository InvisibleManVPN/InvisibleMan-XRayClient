namespace InvisibleManXRay.Services.Analytics.Broadcast
{
    using Models.Analytics;

    public abstract class BroadcastEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public BroadcastEvent()
        {
            Scope = "Broadcast";
            Params = new Param[] { };
        }
    }
}