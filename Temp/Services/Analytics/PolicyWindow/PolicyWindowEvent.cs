namespace InvisibleManXRay.Services.Analytics.PolicyWindow
{
    using Models.Analytics;

    public abstract class PolicyWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public PolicyWindowEvent()
        {
            Scope = "PolicyWindow";
            Params = new Param[] { };
        }
    }
}