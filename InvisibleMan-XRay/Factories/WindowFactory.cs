using System.Windows;

namespace InvisibleManXRay.Factories
{
    using Core;
    using Models;
    using Managers;
    using Handlers;

    public class WindowFactory
    {
        private InvisibleManXRayCore core;
        private HandlersManager handlersManager;

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
                onGitHubClick: linkHandler.OpenGitHubRepositoryLink,
                onBugReportingClick: linkHandler.OpenBugReportingLink,
                onCustomLinkClick: linkHandler.OpenCustomLink
            );
            
            return mainWindow;

            bool IsNeedToShowPolicyWindow() => settingsHandler.UserSettings.GetClientId() == "";
        }

        public SettingsWindow CreateSettingsWindow()
        {
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            NotifyHandler notifyHandler = handlersManager.GetHandler<NotifyHandler>();

            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Setup(
                getMode: settingsHandler.UserSettings.GetMode,
                getProtocol: settingsHandler.UserSettings.GetProtocol,
                getUdpEnabled: settingsHandler.UserSettings.GetUdpEnabled,
                getRunningAtStartupEnabled: settingsHandler.UserSettings.GetRunningAtStartupEnabled,
                getSendingAnalyticsEnabled: settingsHandler.UserSettings.GetSendingAnalyticsEnabled,
                getProxyPort: settingsHandler.UserSettings.GetProxyPort,
                getTunPort: settingsHandler.UserSettings.GetTunPort,
                getTestPort: settingsHandler.UserSettings.GetTestPort,
                getDeviceIp: settingsHandler.UserSettings.GetTunIp,
                getDns: settingsHandler.UserSettings.GetDns,
                getLogLevel: settingsHandler.UserSettings.GetLogLevel,
                getLogPath: settingsHandler.UserSettings.GetLogPath,
                onUpdateUserSettings: UpdateUserSettings
            );

            return settingsWindow;

            void UpdateUserSettings(UserSettings userSettings)
            {
                settingsHandler.UpdateUserSettings(userSettings);
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

        private ServerWindow CreateServerWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            TemplateHandler templateHandler = handlersManager.GetHandler<TemplateHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            MainWindow mainWindow = GetMainWindow();
            
            ServerWindow serverWindow = new ServerWindow();
            serverWindow.Setup(
                getCurrentConfigIndex: settingsHandler.UserSettings.GetCurrentConfigIndex,
                getAllConfigs: configHandler.GetAllConfigs,
                convertConfigLinkToV2Ray: templateHandler.ConverLinkToV2Ray,
                loadConfig: core.LoadConfig,
                testConnection: core.Test,
                getLogPath: settingsHandler.UserSettings.GetLogPath,
                onCopyConfig: configHandler.CopyConfig,
                onCreateConfig: configHandler.CreateConfig,
                onDeleteConfig: configHandler.LoadConfigFiles,
                onUpdateConfig: UpdateConfig
            );
            
            return serverWindow;

            void UpdateConfig(int index)
            {
                settingsHandler.UpdateCurrentConfigIndex(index);
                mainWindow.UpdateUI();
                mainWindow.TryRerun();
            }
        }

        public PolicyWindow CreatePolicyWindow()
        {
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();

            PolicyWindow policyWindow = new PolicyWindow();
            policyWindow.Setup(
                onEmailClick: linkHandler.OpenEmailLink,
                onGenerateClientId: settingsHandler.GenerateClientId
            );

            return policyWindow;
        }
    }
}