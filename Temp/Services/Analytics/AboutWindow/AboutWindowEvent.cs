namespace InvisibleManXRay.Services.Analytics.AboutWindow
{
    using Models.Analytics;

    public abstract class AboutWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public AboutWindowEvent()
        {
            Scope = "AboutWindow";
            Params = new Param[] { };
        }
    }
}