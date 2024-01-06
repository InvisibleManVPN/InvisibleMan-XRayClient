namespace InvisibleManXRay.Services.Analytics.UpdateWindow
{
    using Models.Analytics;

    public abstract class UpdateWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public UpdateWindowEvent()
        {
            Scope = "UpdateWindow";
            Params = new Param[] { };
        }
    }
}