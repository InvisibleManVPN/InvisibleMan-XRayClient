namespace InvisibleManXRay.Services.Analytics.ServerWindow
{
    using Models.Analytics;

    public abstract class ServerWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public ServerWindowEvent()
        {
            Scope = "ServerWindow";
            Params = new Param[] { };
        }
    }
}