namespace InvisibleManXRay.Managers.Initializers
{
    using Core;
    using Managers;
    using Factories;

    public class FactoriesInitializer
    {
        public WindowFactory WindowFactory { get; private set; }

        public void Register()
        {
            WindowFactory = new WindowFactory();
        }

        public void Setup(InvisibleManXRayCore core, HandlersManager handlersManager)
        {
            WindowFactory.Setup(core, handlersManager);
        }
    }
}