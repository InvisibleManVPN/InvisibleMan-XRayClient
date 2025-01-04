using System.Windows;

namespace InvisibleManXRay.Factories
{
    using Core;
    using Models;
    using Managers;
    using Services;
    using Handlers;
    using Values;

    public class WindowFactory
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public void Setup(InvisibleManXRayCore core, HandlersManager handlersManager)
        {
            this.core = core;
            this.handlersManager = handlersManager;
        }

        public MainWindow GetMainWindow() => Application.Current.MainWindow as MainWindow;

        public MainWindow CreateMainWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            UpdateHandler updateHandler = handlersManager.GetHandler<UpdateHandler>();
            BroadcastHandler broadcastHandler = handlersManager.GetHandler<BroadcastHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Setup(
                isNeedToShowPolicyWindow: IsNeedToShowPolicyWindow,
                shouldStartHidden: ShouldStartHidden,
                isNeedToAutoConnect: IsNeedToAutoConnect,
                getConfig: configHandler.GetCurrentConfig,
                loadConfig: core.LoadConfig,
                enableMode: core.EnableMode,
                checkForUpdate: updateHandler.CheckForUpdate,
                checkForBroadcast: broadcastHandler.CheckForBroadcast,
                openServerWindow: CreateServerWindow,
                openSettingsWindow: CreateSettingsWindow,
                openUpdateWindow: CreateUpdateWindow,
                openAboutWindow: CreateAboutWindow,
                openPolicyWindow: CreatePolicyWindow,
                onRunServer: core.Run,
                onStopServer: core.Stop,
                onCancelServer: core.Cancel,
                onDisableMode: core.DisableMode,
                onGenerateClientId: settingsHandler.GenerateClientId,
                onGitHubClick: linkHandler.OpenGitHubRepositoryLink,
                onBugReportingClick: linkHandler.OpenBugReportingLink,
                onCustomLinkClick: linkHandler.OpenCustomLink
            );
            
            return mainWindow;

            bool IsNeedToShowPolicyWindow() => settingsHandler.UserSettings.GetClientId() == "";

            bool ShouldStartHidden() => settingsHandler.UserSettings.GetStartHiddenEnabled();

            bool IsNeedToAutoConnect() => settingsHandler.UserSettings.GetAutoConnectEnabled();
        }

        public SettingsWindow CreateSettingsWindow()
        {
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            NotifyHandler notifyHandler = handlersManager.GetHandler<NotifyHandler>();
            LocalizationHandler localizationHandler = handlersManager.GetHandler<LocalizationHandler>();

            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Setup(
                getLanguage: settingsHandler.UserSettings.GetLanguage,
                getMode: settingsHandler.UserSettings.GetMode,
                getProtocol: settingsHandler.UserSettings.GetProtocol,
                getSystemProxyUsed: settingsHandler.UserSettings.GetSystemProxyUsed,
                getUdpEnabled: settingsHandler.UserSettings.GetUdpEnabled,
                getRunningAtStartupEnabled: settingsHandler.UserSettings.GetRunningAtStartupEnabled,
                getStartHiddenEnabled: settingsHandler.UserSettings.GetStartHiddenEnabled,
                getAutoConnectEnabled: settingsHandler.UserSettings.GetAutoConnectEnabled,
                getSendingAnalyticsEnabled: settingsHandler.UserSettings.GetSendingAnalyticsEnabled,
                getProxyPort: settingsHandler.UserSettings.GetProxyPort,
                getTunPort: settingsHandler.UserSettings.GetTunPort,
                getTestPort: settingsHandler.UserSettings.GetTestPort,
                getDeviceIp: settingsHandler.UserSettings.GetTunIp,
                getDns: settingsHandler.UserSettings.GetDns,
                getLogLevel: settingsHandler.UserSettings.GetLogLevel,
                getLogPath: settingsHandler.UserSettings.GetLogPath,
                openPolicyWindow: CreatePolicyWindow,
                onUpdateUserSettings: UpdateUserSettings
            );

            SetupLocalizedWindowTitle(
                window: settingsWindow,
                term: Localization.WINDOW_TITLE_SETTINGS
            );

            return settingsWindow;

            void UpdateUserSettings(UserSettings userSettings)
            {
                settingsHandler.UpdateUserSettings(userSettings);
                localizationHandler.TryApplyCurrentLanguage();
                notifyHandler.InitializeNotifyIcon();
                notifyHandler.CheckModeItem(userSettings.GetMode());
                GetMainWindow().TryDisableModeAndRerun();
            }
        }

        public UpdateWindow CreateUpdateWindow()
        {
            UpdateHandler updateHandler = handlersManager.GetHandler<UpdateHandler>();
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.Setup(
                checkForUpdate: updateHandler.CheckForUpdate,
                onUpdateClick: linkHandler.OpenLatestReleaseLink
            );

            SetupLocalizedWindowTitle(
                window: updateWindow,
                term: Localization.WINDOW_TITLE_UPDATE
            );

            return updateWindow;
        }

        public AboutWindow CreateAboutWindow()
        {
            VersionHandler versionHandler = handlersManager.GetHandler<VersionHandler>();
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Setup(
                getApplicationVersion: GetApplicationVersion,
                getXRayCoreVersion: GetXRayCoreVersion,
                onEmailClick: linkHandler.OpenEmailLink,
                onWebsiteClick: linkHandler.OpenWebsiteLink,
                onBugReportingClick: linkHandler.OpenBugReportingLink
            );

            SetupLocalizedWindowTitle(
                window: aboutWindow,
                term: Localization.WINDOW_TITLE_ABOUT
            );

            return aboutWindow;

            string GetApplicationVersion()
            {
                AppVersion appVersion = versionHandler.GetApplicationVersion();
                return $"{appVersion.Major}.{appVersion.Feature}.{appVersion.BugFix}";
            }

            string GetXRayCoreVersion()
            {
                return core.GetVersion();
            }
        }

        public ServerWindow CreateServerWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            TemplateHandler templateHandler = handlersManager.GetHandler<TemplateHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            MainWindow mainWindow = GetMainWindow();
            
            ServerWindow serverWindow = new ServerWindow();
            serverWindow.Setup(
                getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath,
                isCurrentPathEqualRootConfigPath: configHandler.IsCurrentPathEqualRootConfigPath,
                getAllGeneralConfigs: configHandler.GetAllGeneralConfigs,
                getAllSubscriptionConfigs: configHandler.GetAllSubscriptionConfigs,
                getAllGroups: configHandler.GetAllGroups,
                convertLinkToConfig: templateHandler.ConverLinkToConfig,
                convertLinkToSubscription: templateHandler.ConvertLinkToSubscription,
                loadConfig: core.LoadConfig,
                testConnection: core.Test,
                getLogPath: settingsHandler.UserSettings.GetLogPath,
                onCopyConfig: configHandler.CopyConfig,
                onCreateConfig: configHandler.CreateConfig,
                onCreateSubscription: configHandler.CreateSubscription,
                onDeleteSubscription: configHandler.DeleteSubscription,
                onDeleteConfig: configHandler.LoadFiles,
                onUpdateConfig: UpdateConfig
            );

            SetupLocalizedWindowTitle(
                window: serverWindow,
                term: Localization.WINDOW_TITLE_SERVER
            );
            
            return serverWindow;

            void UpdateConfig(string path)
            {
                settingsHandler.UpdateCurrentConfigPath(path);
                mainWindow.UpdateUI();
                mainWindow.TryRerun();
            }
        }

        public PolicyWindow CreatePolicyWindow()
        {
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            PolicyWindow policyWindow = new PolicyWindow();
            policyWindow.Setup(
                onEmailClick: linkHandler.OpenEmailLink
            );

            SetupLocalizedWindowTitle(
                window: policyWindow,
                term: Localization.WINDOW_TITLE_POLICY
            );

            return policyWindow;
        }

        private void SetupLocalizedWindowTitle(Window window, string term)
        {
            window.Title = $"Invisible Man XRay - {LocalizationService.GetTerm(term)}";
        }
    }
}