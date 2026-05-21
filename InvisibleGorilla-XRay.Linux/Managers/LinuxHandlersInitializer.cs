using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Handlers.DeepLinks;
using InvisibleGorillaXRay.Handlers.Proxies;
using InvisibleGorillaXRay.Handlers.Settings.Startup;
using InvisibleGorillaXRay.Handlers.Tunnels;
using InvisibleGorillaXRay.Linux.Factories;
using InvisibleGorillaXRay.Linux.Handlers;
using InvisibleGorillaXRay.Linux.Handlers.DeepLinks;
using InvisibleGorillaXRay.Linux.Handlers.Proxies;
using InvisibleGorillaXRay.Linux.Handlers.Settings;
using InvisibleGorillaXRay.Linux.Handlers.Tunnels;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Models;

namespace InvisibleGorillaXRay.Linux.Managers
{
    public class LinuxHandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler(() => new LinuxStartup()));
            HandlersManager.AddHandler(new TemplateHandler());
            HandlersManager.AddHandler(new ProcessHandler());
            HandlersManager.AddHandler(new ConfigHandler());
            HandlersManager.AddHandler(new ProxyHandler(() => new LinuxProxy()));
            HandlersManager.AddHandler(new TunnelHandler(() => new LinuxTunnel()));
            HandlersManager.AddHandler(new LinuxNotifyHandler());
            HandlersManager.AddHandler(new VersionHandler());
            HandlersManager.AddHandler(new UpdateHandler());
            HandlersManager.AddHandler(new BroadcastHandler());
            HandlersManager.AddHandler(new DeepLinkHandler(() => new LinuxDeepLink()));
            HandlersManager.AddHandler(new LinkHandler());
            HandlersManager.AddHandler(new LinuxLocalizationHandler());
        }

        public void Setup(
            InvisibleGorillaXRayCore core,
            HandlersManager handlersManager,
            LinuxWindowFactory windowFactory)
        {
            SetupProcessHandler();
            SetupTunnelHandler();
            SetupConfigHandler();
            SetupNotifyHandler();
            SetupDeepLinkHandler();
            SetupLocalizationHandler();

            void SetupProcessHandler()
            {
                var settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                var processHandler = handlersManager.GetHandler<ProcessHandler>();
                processHandler.Setup(getTunnelPort: settingsHandler.UserSettings.GetTunPort);
            }

            void SetupTunnelHandler()
            {
                var processHandler = handlersManager.GetHandler<ProcessHandler>();
                var tunnelHandler = handlersManager.GetHandler<TunnelHandler>();
                tunnelHandler.Setup(
                    onStartTunnelingService: () => processHandler.TunnelProcess.Start(),
                    isServiceRunning: () => processHandler.TunnelProcess.IsProcessRunning(),
                    isServicePortActive: () => processHandler.TunnelProcess.IsProcessPortActive(),
                    connectTunnelingService: () => processHandler.TunnelProcess.Connect(),
                    executeCommand: command => processHandler.TunnelProcess.Execute(command)
                );
            }

            void SetupConfigHandler()
            {
                var settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                var configHandler = handlersManager.GetHandler<ConfigHandler>();
                configHandler.Setup(
                    getCurrentConfigPath: settingsHandler.UserSettings.GetCurrentConfigPath
                );
            }

            void SetupNotifyHandler()
            {
                var settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                var notifyHandler = handlersManager.GetHandler<LinuxNotifyHandler>();
                notifyHandler.Setup(
                    getMode: settingsHandler.UserSettings.GetMode,
                    onOpenClick: () => OpenApplication(),
                    onAboutClick: () => { },
                    onCloseClick: () => CloseApplication(),
                    onProxyModeClick: () =>
                    {
                        settingsHandler.UpdateMode(Mode.PROXY);
                        windowFactory.GetMainWindow()?.TryDisableModeAndRerun();
                    },
                    onTunnelModeClick: () =>
                    {
                        settingsHandler.UpdateMode(Mode.TUN);
                        windowFactory.GetMainWindow()?.TryDisableModeAndRerun();
                    }
                );
            }

            void SetupDeepLinkHandler()
            {
                var deepLinkHandler = handlersManager.GetHandler<DeepLinkHandler>();
                deepLinkHandler.Setup(
                    onReceiveArg: ref LinuxPipeManager.OnReceiveArg,
                    onConfigLinkFetched: link =>
                    {
                        windowFactory.GetMainWindow()?.UpdateUI();
                    },
                    onSubscriptionLinkFetched: link =>
                    {
                        windowFactory.GetMainWindow()?.UpdateUI();
                    }
                );
            }

            void SetupLocalizationHandler()
            {
                var settingsHandler = handlersManager.GetHandler<SettingsHandler>();
                var locHandler = handlersManager.GetHandler<LinuxLocalizationHandler>();
                locHandler.Setup(getCurrentLanguage: settingsHandler.UserSettings.GetLanguage);
            }
        }

        private void OpenApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is Mac.Views.MainWindow mainWindow)
            {
                mainWindow.ShowAndActivate();
            }
        }

        private void CloseApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }
    }
}
