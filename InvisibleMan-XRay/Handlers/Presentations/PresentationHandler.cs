using Avalonia.Controls;

namespace InvisibleManXRay.Handlers.Presentations
{
    using Definition;

    public abstract class PresentationHandler<T, D> : IHandler
        where T : Window where D : IDefinition
    {
        private readonly T window;
        
        protected T Window => window;

        public PresentationHandler(T window)
        {
            this.window = window;
        }

        public abstract void Setup(D definition);
    }
}