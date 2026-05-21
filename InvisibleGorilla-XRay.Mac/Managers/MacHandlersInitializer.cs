using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using InvisibleGorillaXRay.Core;
using InvisibleGorillaXRay.Handlers;
using InvisibleGorillaXRay.Mac.Factories;
using InvisibleGorillaXRay.Mac.Handlers;
using InvisibleGorillaXRay.Mac.Handlers.DeepLinks;
using InvisibleGorillaXRay.Mac.Handlers.Proxies;
using InvisibleGorillaXRay.Mac.Handlers.Settings;
using InvisibleGorillaXRay.Mac.Handlers.Tunnels;
using InvisibleGorillaXRay.Managers;
using InvisibleGorillaXRay.Models;

namespace InvisibleGorillaXRay.Mac.Managers
{
    public class MacHandlersInitializer
    {
        public HandlersManager HandlersManager { get; private set; }

        public void Register()
        {
            HandlersManager = new HandlersManager();

            HandlersManager.AddHandler(new SettingsHandler(() => new MacStartup()));
            HandlersManager.AddHandler(new TemplateHandler());
            HandlersManager.AddHandler(new ProcessHandler());
            HandlersManager.AddHandler(new ConfigHandler());
            HandlersManager.AddHandler(new ProxyHandler(() => new MacProxy()));
            HandlersManager.AddHandler(new TunnelHandler(() => new MacTunnel()));
            HandlersManager.AddHandler(new MacNotifyHandler());
            HandlersManager.AddHandler(new VersionHandler());
            HandlersManager.AddHandler(new UpdateHandler());
            HandlersManager.AddHandler(new BroadcastHandler());
            HandlersManager.AddHandler(new DeepLinkHandler(() => new MacDeepLink()));
            HandlersManager.AddHandler(new LinkHandler());
            HandlersManager.AddHandler(new MacLocalizationHandler());
        }

        public void Setup(
            InvisibleGorillaXRayCore core,
            HandlersManager handlersManager,
            MacWindowFactory windowFactory)
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
                var notifyHandler = handlersManager.GetHandler<MacNotifyHandler>();
                notifyHandler.Setup(
                    getMode: settingsHandler.UserSettings.GetMode,
                    onOpenClick: () => OpenApplication(),
                    onUpdateClick: () => { },
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
                    onReceiveArg: ref MacPipeManager.OnReceiveArg,
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
                var locHandler = handlersManager.GetHandler<MacLocalizationHandler>();
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
