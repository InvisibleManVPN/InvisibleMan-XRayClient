using System;
using System.Windows;
using System.Threading;

namespace InvisibleManXRay.Managers
{
    using Initializers;
    using Factories;
    using Values;

    public class AppManager
    {
        private CoreInitializer coreInitializer;
        private HandlersInitializer handlersInitializer;
        private FactoriesInitializer factoriesInitializer;

        public WindowFactory WindowFactory => factoriesInitializer.WindowFactory;

        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-S9I1-43bl-A0eM-72A47N6EDH8M}";

        public void Initialize()
        {
            AvoidRunningMultipleInstances();
            SetApplicationCurrentDirectory();

            RegisterCore();
            RegisterHandlers();
            RegisterFactories();

            SetupHandlers();
            SetupCore();
            SetupFactories();
        }

        private void AvoidRunningMultipleInstances()
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            
            if(!isCreatedNew)
            {
                MessageBox.Show(Message.APP_ALREADY_RUNNING);
                Application.Current.Shutdown();
            }
        }

        private void SetApplicationCurrentDirectory()
        {
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(
                path: Environment.ProcessPath
            );
        }

        private void RegisterCore()
        {
            coreInitializer = new CoreInitializer();
            coreInitializer.Register();
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

        private void SetupHandlers()
        {
            handlersInitializer.Setup(
                core: coreInitializer.Core,
                handlersManager: handlersInitializer.HandlersManager,
                windowFactory: factoriesInitializer.WindowFactory
            );
        }

        private void SetupCore()
        {
            coreInitializer.Setup(
                handlersManager: handlersInitializer.HandlersManager
            );
        }

        private void SetupFactories()
        {
            factoriesInitializer.Setup(
                core: coreInitializer.Core,
                handlersManager: handlersInitializer.HandlersManager
            );
        }
    }
}