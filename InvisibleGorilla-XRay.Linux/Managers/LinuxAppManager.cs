using System;
using System.IO;
using System.Threading;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Managers.Initializers;
using InvisibleGorillaXRay.Linux.Factories;
using InvisibleGorillaXRay.Linux.Handlers;
using InvisibleGorillaXRay.Services;

namespace InvisibleGorillaXRay.Linux.Managers
{
    public class LinuxAppManager
    {
        private CoreInitializer coreInitializer;
        private LinuxHandlersInitializer handlersInitializer;
        private ServicesInitializer servicesInitializer;

        public InvisibleGorillaXRayCore Core => coreInitializer.Core;
        public LinuxWindowFactory WindowFactory { get; private set; }
        public HandlersManager HandlersManager => handlersInitializer.HandlersManager;

        private string[] args;
        private static Mutex mutex;
        // Distinct GUID so the Linux build never collides with the Mac/Windows single-instance mutexes.
        private const string APP_GUID = "{7I6N0VI4-LIN-43bl-A0eM-72A47N6EDH8M}";

        public LinuxAppManager(string[] args)
        {
            this.args = args;
        }

        public void Initialize()
        {
            AvoidRunningMultipleInstances();
            SetApplicationCurrentDirectory();

            RegisterCore();
            RegisterHandlers();
            RegisterServices();
            RegisterFactories();

            SetupHandlers();
            SetupServices();
            SetupCore();
            SetupFactories();
            DisableModeByDefault();

            LinuxPipeManager.ListenForPipes();
        }

        private void AvoidRunningMultipleInstances()
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            if (!isCreatedNew)
            {
                if (args.Length != 0)
                    LinuxPipeManager.SignalOpenedApp(args);
                Environment.Exit(0);
            }
        }

        private void SetApplicationCurrentDirectory()
        {
            string runtimeRoot = Path.GetDirectoryName(Environment.ProcessPath)
                ?? AppContext.BaseDirectory;
            string dataRoot = ResolveDataRoot();

            InvisibleGorillaXRay.Values.Directory.ConfigureRoots(dataRoot, runtimeRoot);
            InvisibleGorillaXRay.Values.Directory.EnsureWritableDirectories();

            Environment.CurrentDirectory = runtimeRoot;
            DiagnosticLog.Write(
                "LinuxAppManager",
                $"runtimeRoot={InvisibleGorillaXRay.Values.Directory.RUNTIME_ROOT}, dataRoot={InvisibleGorillaXRay.Values.Directory.DATA_ROOT}");

            static string ResolveDataRoot()
            {
                string dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dataHome))
                {
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    dataHome = Path.Combine(home, ".local", "share");
                }

                return Path.Combine(dataHome, "InvisibleGorilla-XRay");
            }
        }

        private void RegisterCore()
        {
            coreInitializer = new CoreInitializer();
            coreInitializer.Register();
        }

        private void RegisterHandlers()
        {
            handlersInitializer = new LinuxHandlersInitializer();
            handlersInitializer.Register();
        }

        private void RegisterServices()
        {
            servicesInitializer = new ServicesInitializer();
            servicesInitializer.Register();
        }

        private void RegisterFactories()
        {
            WindowFactory = new LinuxWindowFactory();
        }

        private void SetupHandlers()
        {
            handlersInitializer.Setup(coreInitializer.Core, handlersInitializer.HandlersManager, WindowFactory);
        }

        private void SetupServices()
        {
            var locHandler = handlersInitializer.HandlersManager.GetHandler<LinuxLocalizationHandler>();
            servicesInitializer.Setup(
                handlersManager: handlersInitializer.HandlersManager,
                getLocalizedTerm: locHandler.GetTerm
            );
        }

        private void SetupCore()
        {
            coreInitializer.Setup(handlersManager: handlersInitializer.HandlersManager);
        }

        private void SetupFactories()
        {
            WindowFactory.Setup(coreInitializer.Core, handlersInitializer.HandlersManager);
        }

        private void DisableModeByDefault()
        {
            coreInitializer.Core.DisableMode();
        }
    }
}
