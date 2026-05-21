using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Handlers.DeepLinks;
using InvisibleGorillaXRay.Handlers.Proxies;
using InvisibleGorillaXRay.Handlers.Settings.Startup;
using InvisibleGorillaXRay.Handlers.Tunnels;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Managers.Initializers;
using InvisibleGorillaXRay.Mac.Factories;
using InvisibleGorillaXRay.Mac.Handlers;
using InvisibleGorillaXRay.Mac.Handlers.DeepLinks;
using InvisibleGorillaXRay.Mac.Handlers.Proxies;
using InvisibleGorillaXRay.Mac.Handlers.Settings;
using InvisibleGorillaXRay.Mac.Handlers.Tunnels;
using InvisibleGorillaXRay.Services;

namespace InvisibleGorillaXRay.Mac.Managers
{
    public class MacAppManager
    {
        private CoreInitializer coreInitializer;
        private MacHandlersInitializer handlersInitializer;
        private ServicesInitializer servicesInitializer;

        public InvisibleGorillaXRayCore Core => coreInitializer.Core;
        public MacWindowFactory WindowFactory { get; private set; }
        public HandlersManager HandlersManager => handlersInitializer.HandlersManager;

        private string[] args;
        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-MAC-43bl-A0eM-72A47N6EDH8M}";

        public MacAppManager(string[] args)
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
        }

        private void AvoidRunningMultipleInstances()
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            if (!isCreatedNew)
            {
                if (args.Length != 0)
                    MacPipeManager.SignalOpenedApp(args);
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
                "MacAppManager",
                $"runtimeRoot={InvisibleGorillaXRay.Values.Directory.RUNTIME_ROOT}, dataRoot={InvisibleGorillaXRay.Values.Directory.DATA_ROOT}");

            static string ResolveDataRoot()
            {
                string appSupport = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(appSupport))
                    appSupport = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library",
                        "Application Support");

                return Path.Combine(appSupport, "InvisibleGorilla-XRay");
            }
        }

        private void RegisterCore()
        {
            coreInitializer = new CoreInitializer();
            coreInitializer.Register();
        }

        private void RegisterHandlers()
        {
            handlersInitializer = new MacHandlersInitializer();
            handlersInitializer.Register();
        }

        private void RegisterServices()
        {
            servicesInitializer = new ServicesInitializer();
            servicesInitializer.Register();
        }

        private void RegisterFactories()
        {
            WindowFactory = new MacWindowFactory();
        }

        private void SetupHandlers()
        {
            handlersInitializer.Setup(coreInitializer.Core, handlersInitializer.HandlersManager, WindowFactory);
        }

        private void SetupServices()
        {
            var locHandler = handlersInitializer.HandlersManager.GetHandler<MacLocalizationHandler>();
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
