namespace InvisibleManXRay.Factories
{
    using Definition;
    using Handlers.Presentations;
    using Windows;

    public class WindowsFactory
    {
        public MainWindow CreateMainWindow()
        {
            var mainWindow = new MainWindow();
            var presentationHandler = new MainWindowPresentationHandler(mainWindow);
            var definition = new MainWindowDefinition();
            
            presentationHandler.Setup(definition);
            return mainWindow;
        }

        public ConfigWindow CreateConfigWindow()
        {
            return new ConfigWindow();
        }
    }
}