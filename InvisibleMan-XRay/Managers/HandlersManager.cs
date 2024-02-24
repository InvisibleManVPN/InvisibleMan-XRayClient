namespace InvisibleManXRay.Managers
{
    using Foundation;
    using Handlers;

    public class HandlersManager
    {
        private readonly Container<IHandler> handlers;
        private const string TAG = "handler";

        public HandlersManager()
        {
            this.handlers = new Container<IHandler>(TAG);
        }

        public void AddHandler(IHandler handler) => handlers.Add(handler);

        public T GetHandler<T>() where T : IHandler => handlers.Get<T>();
    }
}