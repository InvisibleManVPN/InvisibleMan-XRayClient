namespace InvisibleManXRay.Managers.Initializers
{
    using Factories;

    public class FactoriesInitializer
    {
        public WindowsFactory WindowsFactory { get; private set; }

        public void Register()
        {
            WindowsFactory = new WindowsFactory();
        }
    }
}