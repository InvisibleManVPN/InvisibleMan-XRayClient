namespace InvisibleManXRay.Services.Analytics
{
    using Models.Analytics;

    public interface IEvent
    {
        public string Scope { get; }
        public Param[] Params { get; }
    }
}