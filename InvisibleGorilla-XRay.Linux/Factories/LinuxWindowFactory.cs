using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Models;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Services;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Values;
using InvisibleGorillaXRay.Linux.Handlers;
// The reused views live under the Mac.Views namespace because the .axaml files
// are linked from the Mac project; we keep the using here so this factory can
// instantiate the same MainWindow / SettingsWindow / etc.
using InvisibleGorillaXRay.Mac.Views;

namespace InvisibleGorillaXRay.Linux.Factories
{
    public class LinuxWindowFactory
    {
        private InvisibleGorillaXRayCore core;
        private HandlersManager handlersManager;
        private WindowIcon? _appIcon;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public void Setup(InvisibleGorillaXRayCore core, HandlersManager handlersManager)
        {
            this.core = core;
            this.handlersManager = handlersManager;
        }

        public WindowIcon? GetAppIcon()
        {
            if (_appIcon != null) return _appIcon;
            try
            {
                int size = 256;
                var visual = new Avalonia.Controls.Shapes.Rectangle { Width = size, Height = size };
                if (Application.Current?.TryFindResource("Icon.InvisibleGorilla", out var res) == true && res is IBrush brush)
                    visual.Fill = brush;
                else
                    visual.Fill = new SolidColorBrush(Color.Parse("#4CAF50"));

                visual.Measure(new Size(size, size));
                visual.Arrange(new Rect(0, 0, size, size));

                var rtb = new RenderTargetBitmap(new PixelSize(size, size));
                rtb.Render(visual);

                using var stream = new MemoryStream();
                rtb.Save(stream);
                stream.Position = 0;
                _appIcon = new WindowIcon(stream);
            }
            catch { }
            return _appIcon;
        }

        public MainWindow? GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow as MainWindow;
            return null;
        }

        public MainWindow CreateMainWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            UpdateHandler updateHandler = handlersManager.GetHandler<UpdateHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            MainWindow mainWindow = new MainWindow();
            var icon = GetAppIcon();
            if (icon != null) mainWindow.Icon = icon;

            mainWindow.Setup(
                isNeedToShowPolicyWindow: () => settingsHandler.UserSettings.GetClientId() == "",
                shouldStartHidden: settingsHandler.UserSettings.GetStartHiddenEnabled,
                isNeedToAutoConnect: settingsHandler.UserSettings.GetAutoConnectEnabled,
                getConfig: configHandler.GetCurrentConfig,
                loadConfig: core.LoadConfig,
                enableMode: core.EnableMode,
                checkForUpdate: updateHandler.CheckForUpdate,
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
        }

        public ServerWindow CreateServerWindow()
        {
            ConfigHandler configHandler = handlersManager.GetHandler<ConfigHandler>();
            TemplateHandler templateHandler = handlersManager.GetHandler<TemplateHandler>();
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            MainWindow? mainWindow = GetMainWindow();

            ServerWindow serverWindow = new ServerWindow();
            serverWindow.Setup(
                getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath,
                getUserSettings: () => settingsHandler.UserSettings,
                openAppRulesWindow: CreateAppRulesWindow,
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

            SetupLocalizedWindowTitle(serverWindow, Localization.WINDOW_TITLE_SERVER);
            return serverWindow;

            void UpdateConfig(string path)
            {
                settingsHandler.UpdateCurrentConfigPath(path);
                mainWindow?.UpdateUI();
                mainWindow?.TryRerun();
            }
        }

        public SettingsWindow CreateSettingsWindow()
        {
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            LinuxNotifyHandler notifyHandler = handlersManager.GetHandler<LinuxNotifyHandler>();
            LinuxLocalizationHandler localizationHandler = handlersManager.GetHandler<LinuxLocalizationHandler>();

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
                getUserSettings: () => settingsHandler.UserSettings,
                getAppRulesMode: settingsHandler.UserSettings.GetAppRulesMode,
                getAppRules: settingsHandler.UserSettings.GetAppRules,
                openAppRulesWindow: CreateAppRulesWindow,
                openPolicyWindow: CreatePolicyWindow,
                onUpdateUserSettings: UpdateUserSettings
            );

            SetupLocalizedWindowTitle(settingsWindow, Localization.WINDOW_TITLE_SETTINGS);
            return settingsWindow;

            void UpdateUserSettings(UserSettings userSettings)
            {
                settingsHandler.UpdateUserSettings(userSettings);
                localizationHandler.TryApplyCurrentLanguage();
                notifyHandler.InitializeNotifyIcon();
                notifyHandler.CheckModeItem();
                GetMainWindow()?.TryDisableModeAndRerun();
            }
        }

        public AppRulesWindow CreateAppRulesWindow()
        {
            SettingsHandler settingsHandler = handlersManager.GetHandler<SettingsHandler>();
            LinuxNotifyHandler notifyHandler = handlersManager.GetHandler<LinuxNotifyHandler>();
            LinuxLocalizationHandler localizationHandler = handlersManager.GetHandler<LinuxLocalizationHandler>();

            AppRulesWindow appRulesWindow = new AppRulesWindow();
            var icon = GetAppIcon();
            if (icon != null) appRulesWindow.Icon = icon;

            appRulesWindow.Setup(
                getUserSettings: () => settingsHandler.UserSettings,
                onUpdateUserSettings: UpdateUserSettings
            );

            SetupLocalizedWindowTitle(appRulesWindow, "Lang.AppRules.Title");
            return appRulesWindow;

            void UpdateUserSettings(UserSettings userSettings)
            {
                settingsHandler.UpdateUserSettings(userSettings);
                localizationHandler.TryApplyCurrentLanguage();
                notifyHandler.InitializeNotifyIcon();
                notifyHandler.CheckModeItem();
                GetMainWindow()?.TryDisableModeAndRerun();
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

            SetupLocalizedWindowTitle(updateWindow, Localization.WINDOW_TITLE_UPDATE);
            return updateWindow;
        }

        public AboutWindow CreateAboutWindow()
        {
            VersionHandler versionHandler = handlersManager.GetHandler<VersionHandler>();
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();

            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Setup(
                getApplicationVersion: GetApplicationVersion,
                getXRayCoreVersion: () => core.GetVersion(),
                onEmailClick: linkHandler.OpenEmailLink,
                onWebsiteClick: linkHandler.OpenWebsiteLink,
                onBugReportingClick: linkHandler.OpenBugReportingLink
            );

            SetupLocalizedWindowTitle(aboutWindow, Localization.WINDOW_TITLE_ABOUT);
            return aboutWindow;

            string GetApplicationVersion()
            {
                AppVersion appVersion = versionHandler.GetApplicationVersion();
                return $"{appVersion.Major}.{appVersion.Feature}.{appVersion.BugFix}";
            }
        }

        public PolicyWindow CreatePolicyWindow()
        {
            LinkHandler linkHandler = handlersManager.GetHandler<LinkHandler>();
            PolicyWindow policyWindow = new PolicyWindow();
            policyWindow.Setup(onEmailClick: linkHandler.OpenEmailLink);
            SetupLocalizedWindowTitle(policyWindow, Localization.WINDOW_TITLE_POLICY);
            return policyWindow;
        }

        private void SetupLocalizedWindowTitle(Window window, string term)
        {
            window.Title = $"Invisible Gorilla XRay - {LocalizationService.GetTerm(term)}";
        }
    }
}
