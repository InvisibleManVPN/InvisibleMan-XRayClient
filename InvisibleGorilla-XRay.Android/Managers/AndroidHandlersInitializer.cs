using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Handlers.DeepLinks;
using InvisibleGorillaXRay.Handlers.Settings.Startup;
using InvisibleGorillaXRay.Managers;

namespace InvisibleGorillaXRay.Android.Managers
{
    using InvisibleGorillaXRay.Android.Handlers;
    using InvisibleGorillaXRay.Android.Handlers.DeepLinks;
    using InvisibleGorillaXRay.Android.Handlers.Proxies;
    using InvisibleGorillaXRay.Android.Handlers.Settings;
    using InvisibleGorillaXRay.Android.Handlers.Tunnels;
    using InvisibleGorillaXRay.Models;

    public sealed class AndroidHandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; } = null!;
        public AndroidLocalizationHandler LocalizationHandler { get; private set; } = null!;

        public void Register()
        {
            HandlersManager = new HandlersManager();
            LocalizationHandler = new AndroidLocalizationHandler();

            HandlersManager.AddHandler(new SettingsHandler(() => new AndroidStartup()));
            HandlersManager.AddHandler(new TemplateHandler());
            HandlersManager.AddHandler(new ConfigHandler());
            HandlersManager.AddHandler(new ProxyHandler(() => new AndroidProxy()));
            HandlersManager.AddHandler(new TunnelHandler(() => new AndroidTunnel()));
            HandlersManager.AddHandler(new VersionHandler());
            HandlersManager.AddHandler(new UpdateHandler());
            HandlersManager.AddHandler(new BroadcastHandler());
            HandlersManager.AddHandler(new DeepLinkHandler(() => new AndroidDeepLink()));
            HandlersManager.AddHandler(LocalizationHandler);
        }

        public void Setup()
        {
            SetupTunnelHandler();
            SetupConfigHandler();
            SetupUpdateHandler();
            SetupDeepLinkHandler();
            SetupLocalizationHandler();

            void SetupTunnelHandler()
            {
                HandlersManager.GetHandler<TunnelHandler>().Setup(
                    onStartTunnelingService: () => { },
                    isServiceRunning: () => false,
                    isServicePortActive: () => false,
                    connectTunnelingService: () => new Status(Code.ERROR, SubCode.CANT_TUNNEL, string.Empty),
                    executeCommand: command => new Status(Code.ERROR, SubCode.CANT_TUNNEL, command)
                );
            }

            void SetupConfigHandler()
            {
                SettingsHandler settingsHandler = HandlersManager.GetHandler<SettingsHandler>();
                HandlersManager.GetHandler<ConfigHandler>().Setup(
                    getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath
                );
            }

            void SetupUpdateHandler()
            {
                VersionHandler versionHandler = HandlersManager.GetHandler<VersionHandler>();
                HandlersManager.GetHandler<UpdateHandler>().Setup(
                    getApplicationVersion: versionHandler.GetApplicationVersion,
                    convertToAppVersion: versionHandler.ConvertToAppVersion
                );
            }

            void SetupDeepLinkHandler()
            {
                HandlersManager.GetHandler<DeepLinkHandler>().Setup(
                    ref AndroidDeepLinkDispatcher.OnReceiveArg,
                    onConfigLinkFetched: _ => { },
                    onSubscriptionLinkFetched: _ => { }
                );
            }

            void SetupLocalizationHandler()
            {
                SettingsHandler settingsHandler = HandlersManager.GetHandler<SettingsHandler>();
                LocalizationHandler.Setup(settingsHandler.UserSettings.GetLanguage);
            }
        }
    }
}
