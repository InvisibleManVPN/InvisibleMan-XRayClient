namespace InvisibleManXRay.Services.Analytics.MainWindow
{
    using Models.Analytics;

    public abstract class MainWindowEvent : IEvent
    {
        public string Scope { get; private set; }
        public Param[] Params { get; protected set; }

        public MainWindowEvent()
        {
            Scope = "MainWindow";
            Params = new Param[] { };
        }
    }
}