namespace InvisibleManXRay.Factories
{
    using Core;
    using Managers;

    public class WindowFactory
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;

        public void Setup(InvisibleManXRayCore core, HandlersManager handlersManager)
        {
            this.core = core;
            this.handlersManager = handlersManager;
        }

        public MainWindow CreateMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Setup(
                loadConfig: core.LoadConfig,
                onRunServer: core.Run,
                openServerWindow: CreateServerWindow
            );
            
            return mainWindow;
        }

        private ServerWindow CreateServerWindow()
        {
            ServerWindow serverWindow = new ServerWindow();
            return serverWindow;
        }
    }
}