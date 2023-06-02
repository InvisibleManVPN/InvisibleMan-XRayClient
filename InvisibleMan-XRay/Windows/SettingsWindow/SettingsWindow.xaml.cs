using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay
{
    using Models;

    public partial class SettingsWindow : Window
    {
        private static readonly Dictionary<Mode, string> Modes = new Dictionary<Mode, string>() {
            { Mode.PROXY, "Proxy" },
            { Mode.TUN, "TUN" }
        };

        private static readonly Dictionary<Protocol, string> Protocols = new Dictionary<Protocol, string>() {
            { Protocol.HTTP, "http" },
            { Protocol.SOCKS, "socks" }
        };

        private static readonly Dictionary<LogLevel, string> logLevels = new Dictionary<LogLevel, string>() {
            { LogLevel.NONE, "None" },
            { LogLevel.DEBUG, "Debug" },
            { LogLevel.INFO, "Info"},
            { LogLevel.WARNING, "Warning" },
            { LogLevel.ERROR, "Error" }
        };

        private Func<Mode> getMode;
        private Func<Protocol> getProtocol;
        private Func<bool> getUdpEnabled;
        private Func<bool> getRunAtStartupEnabled;
        private Func<int> getProxyPort;
        private Func<int> getTunPort;
        private Func<int> getTestPort;
        private Func<string> getDeviceIp;
        private Func<String> getDns;
        private Func<LogLevel> getLogLevel;
        private Func<string> getLogPath;

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeItems();

            void InitializeItems()
            {
                InitializeModeItems();
                InitializeProtocolItems();
                InitializeLogLevelItems();

                void InitializeModeItems() => comboBoxMode.ItemsSource = Modes;

                void InitializeProtocolItems() => comboBoxProtocol.ItemsSource = Protocols;

                void InitializeLogLevelItems() => comboBoxLogLevel.ItemsSource = logLevels;
            }
        }

        public void Setup(
            Func<Mode> getMode,
            Func<Protocol> getProtocol,
            Func<bool> getUdpEnabled,
            Func<bool> getRunAtStartupEnabled,
            Func<int> getProxyPort,
            Func<int> getTunPort,
            Func<int> getTestPort,
            Func<string> getDeviceIp,
            Func<string> getDns,
            Func<LogLevel> getLogLevel,
            Func<string> getLogPath
        )
        {
            this.getMode = getMode;
            this.getProtocol = getProtocol;
            this.getUdpEnabled = getUdpEnabled;
            this.getRunAtStartupEnabled = getRunAtStartupEnabled;
            this.getProxyPort = getProxyPort;
            this.getTunPort = getTunPort;
            this.getTestPort = getTestPort;
            this.getDeviceIp = getDeviceIp;
            this.getDns = getDns;
            this.getLogLevel = getLogLevel;
            this.getLogPath = getLogPath;

            UpdateUI();
        }

        private void UpdateUI()
        {
            UpdateBasicPanelUI();
            UpdatePortPanelUI();
            UpdateTunPanelUI();
            UpdateLogPanelUI();

            void UpdateBasicPanelUI()
            {
                comboBoxMode.SelectedValue = getMode.Invoke();
                comboBoxProtocol.SelectedValue = getProtocol.Invoke();
                checkBoxEnableUdp.IsChecked = getUdpEnabled.Invoke();
                checkBoxRunAtStartup.IsChecked = getRunAtStartupEnabled.Invoke();
            }

            void UpdatePortPanelUI()
            {
                textBoxProxyPort.Text = getProxyPort.Invoke().ToString();
                textBoxTunPort.Text = getTunPort.Invoke().ToString();
                textBoxTestPort.Text = getTestPort.Invoke().ToString();
            }

            void UpdateTunPanelUI()
            {
                textBoxTunDeviceIp.Text = getDeviceIp.Invoke();
                textBoxTunDns.Text = getDns.Invoke();
            }

            void UpdateLogPanelUI()
            {
                comboBoxLogLevel.SelectedValue = getLogLevel.Invoke();
                textBoxLogPath.Text = Path.GetFullPath(getLogPath.Invoke());
            }
        }

        private void OnBasicTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableBasicTabButton(false);
            SetActiveBasicPanel(true);
        }

        private void OnPortTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnablePortTabButton(false);
            SetActivePortPanel(true);
        }

        private void OnTunTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableTunTabButton(false);
            SetActiveTunPanel(true);
        }

        private void OnLogTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableLogTabButton(false);
            SetActiveLogPanel(true);
        }

        private void SetActiveBasicPanel(bool isActive) => SetActivePanel(panelBasic, isActive);

        private void SetActivePortPanel(bool isActive) => SetActivePanel(panelPort, isActive);

        private void SetActiveTunPanel(bool isActive) => SetActivePanel(panelTun, isActive);

        private void SetActiveLogPanel(bool isActive) => SetActivePanel(panelLog, isActive);

        private void SetActivePanel(Panel panel, bool isActive)
        {
            panel.Visibility = isActive ? Visibility.Visible : Visibility.Hidden;
        }
        
        private void HideAllPanels()
        {
            SetActiveBasicPanel(false);
            SetActivePortPanel(false);
            SetActiveTunPanel(false);
            SetActiveLogPanel(false);
        }

        private void SetEnableBasicTabButton(bool isEnabled) => SetEnableButton(buttonBasicTab, isEnabled);

        private void SetEnablePortTabButton(bool isEnabled) => SetEnableButton(buttonPortTab, isEnabled);

        private void SetEnableTunTabButton(bool isEnabled) => SetEnableButton(buttonTunTab, isEnabled);

        private void SetEnableLogTabButton(bool isEnabled) => SetEnableButton(buttonLogTab, isEnabled);

        private void SetEnableButton(Button button, bool isEnabled)
        {
            button.IsEnabled = isEnabled;
        }

        private void EnableAllTabs()
        {
            SetEnableBasicTabButton(true);
            SetEnablePortTabButton(true);
            SetEnableTunTabButton(true);
            SetEnableLogTabButton(true);
        }
    }
}