using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;

namespace InvisibleGorillaXRay.Mac.Views
{
    using Models;
    using InvisibleGorillaXRay.Mac.Services;
    using InvisibleGorillaXRay.Services;
    using InvisibleGorillaXRay.Services.Analytics.SettingsWindow;

    public partial class SettingsWindow : Window
    {
        private static readonly Dictionary<string, string> Languages = new()
        {
            { "en-US", "English" },
            { "ru-RU", "Русский" }
        };

        private static readonly Dictionary<Mode, string> Modes = new()
        {
            { Mode.PROXY, "Proxy" },
            { Mode.TUN, "TUN" }
        };

        private static readonly Dictionary<Protocol, string> Protocols = new()
        {
            { Protocol.HTTP, "http" },
            { Protocol.SOCKS, "socks" }
        };

        private static readonly Dictionary<LogLevel, string> LogLevels = new()
        {
            { LogLevel.NONE, "None" },
            { LogLevel.DEBUG, "Debug" },
            { LogLevel.INFO, "Info" },
            { LogLevel.WARNING, "Warning" },
            { LogLevel.ERROR, "Error" }
        };

        private Func<string> getLanguage;
        private Func<Mode> getMode;
        private Func<Protocol> getProtocol;
        private Func<bool> getSystemProxyUsed;
        private Func<bool> getUdpEnabled;
        private Func<bool> getRunningAtStartupEnabled;
        private Func<bool> getStartHiddenEnabled;
        private Func<bool> getAutoConnectEnabled;
        private Func<bool> getSendingAnalyticsEnabled;
        private Func<int> getProxyPort;
        private Func<int> getTunPort;
        private Func<int> getTestPort;
        private Func<string> getDeviceIp;
        private Func<string> getDns;
        private Func<LogLevel> getLogLevel;
        private Func<string> getLogPath;
        private Func<UserSettings> getUserSettings;
        private Func<AppRulesMode> getAppRulesMode;
        private Func<List<AppRule>> getAppRules;
        private Func<AppRulesWindow> openAppRulesWindow;
        private Func<PolicyWindow> openPolicyWindow;

        private Action<UserSettings> onUpdateUserSettings;
        private readonly List<MacInstalledAppInfo> discoveredMacApps = new();
        private readonly Dictionary<string, CheckBox> appRuleToggles = new(StringComparer.OrdinalIgnoreCase);

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeItems();
        }

        private void InitializeItems()
        {
            comboBoxLanguage.ItemsSource = Languages.ToList();
            comboBoxLanguage.DisplayMemberBinding = new Avalonia.Data.Binding("Value");
            comboBoxLanguage.SelectedValueBinding = new Avalonia.Data.Binding("Key");

            comboBoxMode.ItemsSource = Modes.ToList();
            comboBoxMode.DisplayMemberBinding = new Avalonia.Data.Binding("Value");
            comboBoxMode.SelectedValueBinding = new Avalonia.Data.Binding("Key");

            comboBoxProtocol.ItemsSource = Protocols.ToList();
            comboBoxProtocol.DisplayMemberBinding = new Avalonia.Data.Binding("Value");
            comboBoxProtocol.SelectedValueBinding = new Avalonia.Data.Binding("Key");

            comboBoxLogLevel.ItemsSource = LogLevels.ToList();
            comboBoxLogLevel.DisplayMemberBinding = new Avalonia.Data.Binding("Value");
            comboBoxLogLevel.SelectedValueBinding = new Avalonia.Data.Binding("Key");
        }

        public void Setup(
            Func<string> getLanguage,
            Func<Mode> getMode,
            Func<Protocol> getProtocol,
            Func<bool> getSystemProxyUsed,
            Func<bool> getUdpEnabled,
            Func<bool> getRunningAtStartupEnabled,
            Func<bool> getStartHiddenEnabled,
            Func<bool> getAutoConnectEnabled,
            Func<bool> getSendingAnalyticsEnabled,
            Func<int> getProxyPort,
            Func<int> getTunPort,
            Func<int> getTestPort,
            Func<string> getDeviceIp,
            Func<string> getDns,
            Func<LogLevel> getLogLevel,
            Func<string> getLogPath,
            Func<UserSettings> getUserSettings,
            Func<AppRulesMode> getAppRulesMode,
            Func<List<AppRule>> getAppRules,
            Func<AppRulesWindow> openAppRulesWindow,
            Func<PolicyWindow> openPolicyWindow,
            Action<UserSettings> onUpdateUserSettings)
        {
            this.getLanguage = getLanguage;
            this.getMode = getMode;
            this.getProtocol = getProtocol;
            this.getSystemProxyUsed = getSystemProxyUsed;
            this.getUdpEnabled = getUdpEnabled;
            this.getRunningAtStartupEnabled = getRunningAtStartupEnabled;
            this.getStartHiddenEnabled = getStartHiddenEnabled;
            this.getAutoConnectEnabled = getAutoConnectEnabled;
            this.getSendingAnalyticsEnabled = getSendingAnalyticsEnabled;
            this.getProxyPort = getProxyPort;
            this.getTunPort = getTunPort;
            this.getTestPort = getTestPort;
            this.getDeviceIp = getDeviceIp;
            this.getDns = getDns;
            this.getLogLevel = getLogLevel;
            this.getLogPath = getLogPath;
            this.getUserSettings = getUserSettings;
            this.getAppRulesMode = getAppRulesMode;
            this.getAppRules = getAppRules;
            this.openAppRulesWindow = openAppRulesWindow;
            this.openPolicyWindow = openPolicyWindow;
            this.onUpdateUserSettings = onUpdateUserSettings;

            UpdateUI();
        }

        private void UpdateUI()
        {
            SelectComboBoxItem(comboBoxLanguage, getLanguage.Invoke());
            SelectComboBoxItem(comboBoxMode, getMode.Invoke());
            SelectComboBoxItem(comboBoxProtocol, getProtocol.Invoke());

            checkBoxUseSystemProxy.IsChecked = getSystemProxyUsed.Invoke();
            checkBoxEnableUdp.IsChecked = getUdpEnabled.Invoke();
            checkBoxRunAtStartup.IsChecked = getRunningAtStartupEnabled.Invoke();
            checkBoxStartHidden.IsChecked = getStartHiddenEnabled.Invoke();
            checkBoxAutoConnect.IsChecked = getAutoConnectEnabled.Invoke();
            checkBoxSendAnalytics.IsChecked = getSendingAnalyticsEnabled.Invoke();

            textBoxProxyPort.Text = getProxyPort.Invoke().ToString();
            textBoxTunPort.Text = getTunPort.Invoke().ToString();
            textBoxTestPort.Text = getTestPort.Invoke().ToString();

            textBoxTunDeviceIp.Text = getDeviceIp.Invoke();
            textBoxTunDns.Text = getDns.Invoke();

            SelectComboBoxItem(comboBoxLogLevel, getLogLevel.Invoke());
            textBoxLogPath.Text = Path.GetFullPath(getLogPath.Invoke());
            RefreshAppRulesSummary();
        }

        private void SelectComboBoxItem<T>(ComboBox comboBox, T key)
        {
            var items = comboBox.ItemsSource as System.Collections.IEnumerable;
            if (items == null) return;

            int index = 0;
            foreach (var item in items)
            {
                if (item is KeyValuePair<T, string> kvp && EqualityComparer<T>.Default.Equals(kvp.Key, key))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
                index++;
            }
        }

        private T GetComboBoxSelectedKey<T>(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is KeyValuePair<T, string> kvp)
                return kvp.Key;
            return default;
        }

        private void ReloadDiscoveredApps()
        {
            HashSet<string> selectedIds = GetCurrentSelectedAppIdSet();
            discoveredMacApps.Clear();
            discoveredMacApps.AddRange(MacInstalledAppDiscovery.GetApps());
            RenderMacAppRules(selectedIds);
        }

        private void RenderMacAppRules(ISet<string> selectedIds)
        {
            panelAppRulesItems.Children.Clear();
            appRuleToggles.Clear();

            foreach (MacInstalledAppInfo app in discoveredMacApps)
            {
                CheckBox toggle = new()
                {
                    IsChecked = selectedIds.Contains(app.AppId),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = Brushes.White
                };

                appRuleToggles[app.AppId] = toggle;

                StackPanel textPanel = new()
                {
                    Spacing = 2
                };
                textPanel.Children.Add(new TextBlock
                {
                    Text = app.DisplayName,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                });
                textPanel.Children.Add(new TextBlock
                {
                    Text = app.AppId,
                    Foreground = new SolidColorBrush(Color.Parse("#C9C9C9")),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                });

                Grid contentGrid = new();
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetColumn(toggle, 0);
                Grid.SetColumn(textPanel, 1);
                contentGrid.Children.Add(toggle);
                contentGrid.Children.Add(textPanel);

                Border card = new()
                {
                    Padding = new Thickness(10, 8),
                    BorderBrush = new SolidColorBrush(Color.Parse("#6E6E6E")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Child = contentGrid
                };

                card.PointerPressed += (_, args) =>
                {
                    if (args.Source is CheckBox)
                        return;

                    toggle.IsChecked = !(toggle.IsChecked ?? false);
                };

                panelAppRulesItems.Children.Add(card);
            }

            textBlockNoDiscoveredApps.IsVisible = discoveredMacApps.Count == 0;
        }

        private HashSet<string> GetSavedSelectedAppIdSet()
        {
            return getAppRules.Invoke()
                .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.AppId))
                .Select(rule => rule.AppId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private HashSet<string> GetCurrentSelectedAppIdSet()
        {
            if (appRuleToggles.Count == 0)
                return GetSavedSelectedAppIdSet();

            return appRuleToggles
                .Where(pair => pair.Value.IsChecked == true)
                .Select(pair => pair.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private List<AppRule> BuildSelectedDesktopAppRules()
        {
            if (appRuleToggles.Count == 0)
                return getAppRules.Invoke();

            HashSet<string> selectedIds = GetCurrentSelectedAppIdSet();
            List<AppRule> rules = discoveredMacApps
                .Where(app => selectedIds.Contains(app.AppId))
                .Select(app => new AppRule(
                    appId: app.AppId,
                    displayName: app.DisplayName,
                    iconRef: app.IconRef,
                    enabled: true))
                .ToList();

            foreach (AppRule existingRule in getAppRules.Invoke())
            {
                if (!existingRule.Enabled || string.IsNullOrWhiteSpace(existingRule.AppId))
                    continue;

                if (!selectedIds.Contains(existingRule.AppId))
                    continue;

                if (rules.Any(rule => string.Equals(rule.AppId, existingRule.AppId, StringComparison.OrdinalIgnoreCase)))
                    continue;

                rules.Add(existingRule.Clone());
            }

            return rules;
        }

        private void OnBasicTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonBasicTab.IsEnabled = false;
            panelBasic.IsVisible = true;
        }

        private void OnPortTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonPortTab.IsEnabled = false;
            panelPort.IsVisible = true;
        }

        private void OnTunTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonTunTab.IsEnabled = false;
            panelTun.IsVisible = true;
        }

        private void OnLogTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonLogTab.IsEnabled = false;
            panelLog.IsVisible = true;
        }

        private void OnModeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mode mode = GetComboBoxSelectedKey<Mode>(comboBoxMode);
            comboBoxProtocol.IsEnabled = mode != Mode.TUN;
            checkBoxUseSystemProxy.IsEnabled = mode != Mode.TUN;
            checkBoxEnableUdp.IsEnabled = mode == Mode.TUN;
        }

        private void OnConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            UserSettings currentSettings = getUserSettings.Invoke();
            UserSettings userSettings = new UserSettings(
                language: GetComboBoxSelectedKey<string>(comboBoxLanguage) ?? "en-US",
                mode: GetComboBoxSelectedKey<Mode>(comboBoxMode),
                protocol: GetComboBoxSelectedKey<Protocol>(comboBoxProtocol),
                logLevel: GetComboBoxSelectedKey<LogLevel>(comboBoxLogLevel),
                isSystemProxyUse: checkBoxUseSystemProxy.IsChecked == true,
                isUdpEnable: checkBoxEnableUdp.IsChecked == true,
                isRunningAtStartup: checkBoxRunAtStartup.IsChecked == true,
                isStartHidden: checkBoxStartHidden.IsChecked == true,
                isAutoConnect: checkBoxAutoConnect.IsChecked == true,
                isSendingAnalytics: checkBoxSendAnalytics.IsChecked == true,
                proxyPort: int.TryParse(textBoxProxyPort.Text, out int pp) ? pp : 10801,
                tunPort: int.TryParse(textBoxTunPort.Text, out int tp) ? tp : 10802,
                testPort: int.TryParse(textBoxTestPort.Text, out int tep) ? tep : 10803,
                tunIp: textBoxTunDeviceIp.Text ?? "10.0.236.10",
                dns: textBoxTunDns.Text ?? "8.8.8.8",
                logPath: textBoxLogPath.Text ?? "./Logs",
                appRulesMode: currentSettings.GetAppRulesMode(),
                appRules: currentSettings.GetAppRules(),
                appRuleTemplates: currentSettings.GetAppRuleTemplates(),
                appRuleTemplateBindings: currentSettings.GetAppRuleTemplateBindings()
            );

            SendRunAtStartupActivationEvent(userSettings);
            ForceSendAnalyticsActivationEvent(userSettings);
            onUpdateUserSettings.Invoke(userSettings);

            Close();
        }

        private void SendRunAtStartupActivationEvent(UserSettings userSettings)
        {
            if (getRunningAtStartupEnabled.Invoke() == (checkBoxRunAtStartup.IsChecked == true))
                return;

            if (userSettings.GetRunningAtStartupEnabled())
                AnalyticsService.SendEvent(new RunAtStartupActivatedEvent());
            else
                AnalyticsService.SendEvent(new RunAtStartupDeactivatedEvent());
        }

        private void ForceSendAnalyticsActivationEvent(UserSettings userSettings)
        {
            if (getSendingAnalyticsEnabled.Invoke() == (checkBoxSendAnalytics.IsChecked == true))
                return;

            if (userSettings.GetSendingAnalyticsEnabled())
                AnalyticsService.SendEvent(new AnalyticsActivatedEvent(), true);
            else
                AnalyticsService.SendEvent(new AnalyticsDeactivatedEvent(), true);
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnRefreshAppRulesClick(object sender, RoutedEventArgs e)
        {
            RefreshAppRulesSummary();
        }

        private async void OnManageAppRulesClick(object sender, RoutedEventArgs e)
        {
            AppRulesWindow appRulesWindow = openAppRulesWindow.Invoke();
            appRulesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            await appRulesWindow.ShowDialog(this);
            RefreshAppRulesSummary();
        }

        private void RefreshAppRulesSummary()
        {
            UserSettings settings = getUserSettings.Invoke();
            AppRulesMode mode = settings.GetEffectiveAppRulesMode();
            AppRuleTemplate template = settings.GetEffectiveAppRuleTemplate();
            int selectedCount = settings.GetEffectiveEnabledAppRules().Count;

            textBlockAppRulesSummary.Text = string.Format(
                Localize("Lang.AppRules.Summary"),
                GetTemplateDisplayName(settings.GetBoundAppRuleTemplateId(), template),
                LocalizeMode(mode),
                selectedCount);

            textBlockAppRulesConfigHint.Text = string.IsNullOrWhiteSpace(settings.GetCurrentConfigPath())
                ? Localize("Lang.AppRules.NoConfigSelected")
                : string.Format(
                    Localize("Lang.AppRules.BoundConfig"),
                    settings.GetCurrentConfigPath());
        }

        private string GetTemplateDisplayName(string templateId, AppRuleTemplate template)
        {
            if (string.IsNullOrWhiteSpace(templateId)
                || string.Equals(templateId, AppRuleTemplate.DefaultTemplateId, StringComparison.OrdinalIgnoreCase))
            {
                return Localize("Lang.AppRules.Template.Default");
            }

            return string.IsNullOrWhiteSpace(template.Name)
                ? Localize("Lang.AppRules.Template.Unnamed")
                : template.Name;
        }

        private string LocalizeMode(AppRulesMode mode)
        {
            return mode switch
            {
                AppRulesMode.BYPASS_SELECTED_APPS => Localize("Lang.AppRules.Mode.Bypass"),
                AppRulesMode.ONLY_SELECTED_APPS => Localize("Lang.AppRules.Mode.OnlySelected"),
                _ => Localize("Lang.AppRules.Mode.AllApps")
            };
        }

        private string Localize(string key)
        {
            if (Application.Current?.TryFindResource(key, out object? value) == true)
                return value?.ToString() ?? key;

            return key;
        }

        private void HideAllPanels()
        {
            panelBasic.IsVisible = false;
            panelPort.IsVisible = false;
            panelTun.IsVisible = false;
            panelLog.IsVisible = false;
        }

        private void EnableAllTabs()
        {
            buttonBasicTab.IsEnabled = true;
            buttonPortTab.IsEnabled = true;
            buttonTunTab.IsEnabled = true;
            buttonLogTab.IsEnabled = true;
        }
    }
}
