using System;
using System.Threading;

namespace InvisibleManXRay.Managers
{
    using Foundation;
    using Initializers;
    using Values;

    public class AppManager
    {
        private HandlersInitializer handlersInitializer;
        private FactoriesInitializer factoriesInitializer;
        private ServicesInitializer servicesInitializer;

        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-S9I1-43bl-A0eM-72A47N6EDH8M}";

        public void Initialize(Action onComplete)
        {
            RegisterHandlers();
            RegisterFactories();
            RegisterServices();
            
            SetupHandlers();
            SetupServices();

            AvoidRunningMultipleInstances(
                onCreatedNew: () => {
                    PrepareToContinueApp();
                    onComplete.Invoke();
                },
                onAlreadyRunning: () => {
                    PrepareToExitApp();
                }
            );
        }

        private void RegisterHandlers()
        {
            handlersInitializer = new HandlersInitializer();
            handlersInitializer.Register();
        }

        private void RegisterFactories()
        {
            factoriesInitializer = new FactoriesInitializer();
            factoriesInitializer.Register();
        }

        private void RegisterServices()
        {
            servicesInitializer = new ServicesInitializer();
            servicesInitializer.Register();
        }

        private void SetupHandlers()
        {
            handlersInitializer.Setup();
        }

        private void SetupServices()
        {
            servicesInitializer.Setup(
                windowsFactory: factoriesInitializer.WindowsFactory
            );
        }

        private void AvoidRunningMultipleInstances(
            Action onCreatedNew,
            Action onAlreadyRunning
        )
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            
            if(isCreatedNew)
                onCreatedNew.Invoke();
            else
                MessageBox.Show(
                    message: Message.APP_ALREADY_RUNNING,
                    onResult: (result) => onAlreadyRunning.Invoke()
                );
        }

        private void PrepareToContinueApp()
        {
            SetApplicationCurrentDirectory();

            void SetApplicationCurrentDirectory()
            {
                Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(
                    path: Environment.ProcessPath
                );
            }
        }

        private void PrepareToExitApp()
        {
            Environment.Exit(0);
        }
    }
}