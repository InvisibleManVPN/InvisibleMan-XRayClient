using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Managers.Initializers;

namespace InvisibleGorillaXRay.Android.Managers
{
    public sealed class AndroidAppManager
    {
        private readonly CoreInitializer coreInitializer;
        private readonly AndroidHandlersInitializer handlersInitializer;
        private readonly ServicesInitializer servicesInitializer;

        public AndroidAppManager()
        {
            coreInitializer = new CoreInitializer();
            handlersInitializer = new AndroidHandlersInitializer();
            servicesInitializer = new ServicesInitializer();
        }

        public InvisibleGorillaXRayCore Core => coreInitializer.Core;
        public HandlersManager HandlersManager => handlersInitializer.HandlersManager;

        public void Initialize()
        {
            InvisibleGorillaXRay.Values.Directory.EnsureWritableDirectories();

            coreInitializer.Register();
            handlersInitializer.Register();
            servicesInitializer.Register();

            handlersInitializer.Setup();
            servicesInitializer.Setup(
                handlersManager: handlersInitializer.HandlersManager,
                getLocalizedTerm: handlersInitializer.LocalizationHandler.GetTerm
            );
            coreInitializer.Setup(handlersInitializer.HandlersManager);

            ApplyAndroidDefaults();
            coreInitializer.Core.DisableMode();
        }

        private void ApplyAndroidDefaults()
        {
            SettingsHandler settingsHandler = handlersInitializer.HandlersManager.GetHandler<SettingsHandler>();

            if (string.IsNullOrWhiteSpace(settingsHandler.UserSettings.GetClientId()))
                settingsHandler.GenerateClientId();

            settingsHandler.UpdateUserSettings(new InvisibleGorillaXRay.Models.UserSettings
            {
                Language = settingsHandler.UserSettings.GetLanguage(),
                Mode = InvisibleGorillaXRay.Models.Mode.TUN,
                // Android VpnService routing uses the local SOCKS listener as the bridge target.
                Protocol = InvisibleGorillaXRay.Models.Protocol.SOCKS,
                LogLevel = settingsHandler.UserSettings.GetLogLevel(),
                IsSystemProxyUse = false,
                IsUdpEnable = settingsHandler.UserSettings.GetUdpEnabled(),
                IsRunningAtStartup = false,
                IsStartHidden = false,
                IsAutoConnect = false,
                IsSendingAnalytics = settingsHandler.UserSettings.GetSendingAnalyticsEnabled(),
                ProxyPort = settingsHandler.UserSettings.GetProxyPort(),
                TunPort = settingsHandler.UserSettings.GetTunPort(),
                TestPort = settingsHandler.UserSettings.GetTestPort(),
                TunIp = settingsHandler.UserSettings.GetTunIp(),
                Dns = settingsHandler.UserSettings.GetDns(),
                LogPath = settingsHandler.UserSettings.GetLogPath(),
                AppRulesMode = settingsHandler.UserSettings.GetAppRulesMode(),
                AppRules = settingsHandler.UserSettings.GetAppRules(),
                AppRuleTemplates = settingsHandler.UserSettings.GetAppRuleTemplates(),
                AppRuleTemplateBindings = settingsHandler.UserSettings.GetAppRuleTemplateBindings()
            });
        }
    }
}
