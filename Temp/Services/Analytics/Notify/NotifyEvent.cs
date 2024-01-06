namespace InvisibleManXRay.Services.Analytics.Notify
{
    using Models.Analytics;

    public abstract class NotifyEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public NotifyEvent()
        {
            Scope = "Notify";
            Params = new Param[] { };
        }
    }
}