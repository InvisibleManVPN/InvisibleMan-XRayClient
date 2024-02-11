namespace InvisibleManXRay.Factories
{
    using Windows;

    public class WindowsFactory
    {
        public MainWindow CreateMainWindow()
        {
            return new MainWindow();
        }
    }
}