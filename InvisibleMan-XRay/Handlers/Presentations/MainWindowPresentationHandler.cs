namespace InvisibleManXRay.Handlers.Presentations
{
    using Definition;
    using Services;
    using Windows;

    public class MainWindowPresentationHandler : PresentationHandler<MainWindow, MainWindowDefinition>
    {
        public MainWindowPresentationHandler(MainWindow mainWindow) : base(mainWindow)
        {
        }

        public override void Setup(MainWindowDefinition definition)
        {
            Window.Setup(
                onManageServersClick: OpenConfigWindow
            );
        }

        private void OpenConfigWindow()
        {
            var ConfigWindow = ServiceLocator.Find<WindowsService>().CreateWindow<ConfigWindow>();
            ConfigWindow.ShowDialog(Window);
        }
    }
}