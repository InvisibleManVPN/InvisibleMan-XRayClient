using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InvisibleGorillaXRay
{
    using Models;
    using Services;

    public partial class AppRulesWindow : Window
    {
        private sealed class TemplateComboItem
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;
        }

        private Func<UserSettings>? getUserSettings;
        private Action<UserSettings>? onUpdateUserSettings;
        private readonly List<WindowsInstalledAppInfo> discoveredWindowsApps = new();
        private readonly Dictionary<string, CheckBox> appRuleToggles = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AppRuleTemplate> workingCustomTemplates = new();
        private readonly List<AppRuleTemplateBinding> workingBindings = new();
        private AppRuleTemplate workingDefaultTemplate = new();
        private string activeTemplateId = AppRuleTemplate.DefaultTemplateId;
        private string currentConfigPath = string.Empty;
        private bool isApplyingTemplate;
        private bool isReady;

        public AppRulesWindow()
        {
            InitializeComponent();
        }

        public void Setup(Func<UserSettings> getUserSettings, Action<UserSettings> onUpdateUserSettings)
        {
            this.getUserSettings = getUserSettings;
            this.onUpdateUserSettings = onUpdateUserSettings;

            LoadState();
            isReady = true;
        }

        private void LoadState()
        {
            UserSettings settings = RequireCurrentSettings();
            currentConfigPath = settings.GetCurrentConfigPath();

            workingDefaultTemplate = settings.GetAppRuleTemplateById(AppRuleTemplate.DefaultTemplateId);
            workingCustomTemplates.Clear();
            workingCustomTemplates.AddRange(settings.GetAppRuleTemplates());

            workingBindings.Clear();
            workingBindings.AddRange(settings.GetAppRuleTemplateBindings());

            discoveredWindowsApps.Clear();
            discoveredWindowsApps.AddRange(WindowsInstalledAppDiscovery.GetApps());

            textBlockCurrentConfig.Text = string.IsNullOrWhiteSpace(currentConfigPath)
                ? Localize("Lang.AppRules.NoConfigSelected")
                : currentConfigPath;
            textBoxSearch.Text = string.Empty;

            string selectedTemplateId = settings.GetBoundAppRuleTemplateId(currentConfigPath);
            PopulateTemplateSelector(selectedTemplateId);
        }

        private void PopulateTemplateSelector(string preferredTemplateId)
        {
            List<TemplateComboItem> items = new();
            items.Add(new TemplateComboItem
            {
                Id = AppRuleTemplate.DefaultTemplateId,
                Name = Localize("Lang.AppRules.Template.Default")
            });

            items.AddRange(
                workingCustomTemplates.Select(template => new TemplateComboItem
                {
                    Id = template.Id,
                    Name = GetTemplateDisplayName(template)
                }));

            comboBoxTemplate.ItemsSource = items;

            string selectedTemplateId = items.Any(item => string.Equals(item.Id, preferredTemplateId, StringComparison.OrdinalIgnoreCase))
                ? preferredTemplateId
                : AppRuleTemplate.DefaultTemplateId;

            isApplyingTemplate = true;
            comboBoxTemplate.SelectedValue = selectedTemplateId;
            activeTemplateId = selectedTemplateId;
            ApplyTemplateToEditor(GetTemplateById(activeTemplateId));
            isApplyingTemplate = false;
        }

        private void ApplyTemplateToEditor(AppRuleTemplate template)
        {
            isApplyingTemplate = true;
            try
            {
                textBoxTemplateName.Text = template.Name ?? string.Empty;
                textBoxTemplateName.IsEnabled = !IsDefaultTemplate(template.Id);
                buttonDeleteTemplate.IsEnabled = !IsDefaultTemplate(template.Id);

                switch (template.Mode)
                {
                    case AppRulesMode.BYPASS_SELECTED_APPS:
                        radioButtonBypassApps.IsChecked = true;
                        break;
                    case AppRulesMode.ONLY_SELECTED_APPS:
                        radioButtonOnlySelectedApps.IsChecked = true;
                        break;
                    default:
                        radioButtonAllApps.IsChecked = true;
                        break;
                }

                RenderDiscoveredApps(template);
            }
            finally
            {
                isApplyingTemplate = false;
            }
        }

        private void RenderDiscoveredApps(AppRuleTemplate template)
        {
            panelAppItems.Children.Clear();
            appRuleToggles.Clear();

            string filter = textBoxSearch.Text?.Trim() ?? string.Empty;
            HashSet<string> selectedPaths = template.AppRules
                .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.AppId))
                .Select(rule => rule.AppId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IEnumerable<WindowsInstalledAppInfo> filteredApps = discoveredWindowsApps;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filteredApps = filteredApps.Where(app =>
                    app.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || app.ExecutablePath.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (WindowsInstalledAppInfo app in filteredApps)
            {
                CheckBox toggle = new()
                {
                    IsChecked = selectedPaths.Contains(app.ExecutablePath),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = Brushes.White
                };

                appRuleToggles[app.ExecutablePath] = toggle;

                TextBlock title = new()
                {
                    Text = app.DisplayName,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                };

                TextBlock meta = new()
                {
                    Text = app.ExecutablePath,
                    Foreground = new SolidColorBrush(Color.FromRgb(201, 201, 201)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                };

                StackPanel textPanel = new();
                textPanel.Children.Add(title);
                textPanel.Children.Add(meta);

                Grid contentGrid = new();
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(toggle, 0);
                Grid.SetColumn(textPanel, 1);
                contentGrid.Children.Add(toggle);
                contentGrid.Children.Add(textPanel);

                Border card = new()
                {
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(110, 110, 110)),
                    BorderThickness = new Thickness(1),
                    Child = contentGrid
                };

                card.MouseLeftButtonUp += (_, eventArgs) =>
                {
                    if (eventArgs.OriginalSource is CheckBox)
                        return;

                    toggle.IsChecked = !(toggle.IsChecked ?? false);
                };
                panelAppItems.Children.Add(card);
            }

            textBlockNoApps.Visibility = panelAppItems.Children.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CaptureActiveTemplateState()
        {
            if (isApplyingTemplate)
                return;

            AppRuleTemplate template = GetTemplateById(activeTemplateId);
            template.Mode = GetSelectedMode();
            template.AppRules = BuildSelectedAppRules(template);

            if (!IsDefaultTemplate(template.Id))
                template.Name = textBoxTemplateName.Text?.Trim() ?? string.Empty;
        }

        private List<AppRule> BuildSelectedAppRules(AppRuleTemplate template)
        {
            HashSet<string> selectedIds = appRuleToggles.Count == 0
                ? template.AppRules
                    .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.AppId))
                    .Select(rule => rule.AppId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : appRuleToggles
                    .Where(pair => pair.Value.IsChecked == true)
                    .Select(pair => pair.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<AppRule> rules = discoveredWindowsApps
                .Where(app => selectedIds.Contains(app.ExecutablePath))
                .Select(app => new AppRule(
                    appId: app.ExecutablePath,
                    displayName: app.DisplayName,
                    iconRef: app.IconRef,
                    enabled: true))
                .ToList();

            foreach (AppRule existingRule in template.AppRules)
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

        private AppRuleTemplate GetTemplateById(string templateId)
        {
            if (IsDefaultTemplate(templateId))
                return workingDefaultTemplate;

            AppRuleTemplate? template = workingCustomTemplates.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, templateId, StringComparison.OrdinalIgnoreCase));

            return template ?? workingDefaultTemplate;
        }

        private AppRulesMode GetSelectedMode()
        {
            if (radioButtonOnlySelectedApps.IsChecked == true)
                return AppRulesMode.ONLY_SELECTED_APPS;

            if (radioButtonBypassApps.IsChecked == true)
                return AppRulesMode.BYPASS_SELECTED_APPS;

            return AppRulesMode.ALL_APPS;
        }

        private void OnTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
            activeTemplateId = (comboBoxTemplate.SelectedValue as string) ?? AppRuleTemplate.DefaultTemplateId;
            ApplyTemplateToEditor(GetTemplateById(activeTemplateId));
        }

        private void OnTemplateNameChanged(object sender, TextChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate || IsDefaultTemplate(activeTemplateId))
                return;

            CaptureActiveTemplateState();
            GetTemplateById(activeTemplateId).Name = textBoxTemplateName.Text?.Trim() ?? string.Empty;
            PopulateTemplateSelector(activeTemplateId);
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
            RenderDiscoveredApps(GetTemplateById(activeTemplateId));
        }

        private void OnRefreshAppsClick(object sender, RoutedEventArgs e)
        {
            CaptureActiveTemplateState();

            discoveredWindowsApps.Clear();
            discoveredWindowsApps.AddRange(WindowsInstalledAppDiscovery.GetApps());
            RenderDiscoveredApps(GetTemplateById(activeTemplateId));
        }

        private void OnNewTemplateClick(object sender, RoutedEventArgs e)
        {
            CaptureActiveTemplateState();

            AppRuleTemplate source = GetTemplateById(activeTemplateId).Clone();
            source.Id = Guid.NewGuid().ToString("N");
            source.Name = BuildNewTemplateName();
            workingCustomTemplates.Add(source);

            PopulateTemplateSelector(source.Id);
        }

        private void OnDeleteTemplateClick(object sender, RoutedEventArgs e)
        {
            if (IsDefaultTemplate(activeTemplateId))
                return;

            workingCustomTemplates.RemoveAll(template =>
                string.Equals(template.Id, activeTemplateId, StringComparison.OrdinalIgnoreCase));
            activeTemplateId = AppRuleTemplate.DefaultTemplateId;
            PopulateTemplateSelector(activeTemplateId);
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            CaptureActiveTemplateState();

            UserSettings currentSettings = RequireCurrentSettings();
            List<AppRuleTemplate> templates = workingCustomTemplates
                .Select(template => template.Clone())
                .Where(template => !string.IsNullOrWhiteSpace(template.Id))
                .ToList();

            HashSet<string> validTemplateIds = templates
                .Select(template => template.Id)
                .Append(AppRuleTemplate.DefaultTemplateId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string normalizedCurrentConfigPath = NormalizeConfigPath(currentConfigPath);
            List<AppRuleTemplateBinding> bindings = workingBindings
                .Select(binding => binding.Clone())
                .Where(binding => !string.Equals(
                    NormalizeConfigPath(binding.ConfigPath),
                    normalizedCurrentConfigPath,
                    StringComparison.OrdinalIgnoreCase))
                .Where(binding => validTemplateIds.Contains(binding.TemplateId))
                .ToList();

            if (!string.IsNullOrWhiteSpace(normalizedCurrentConfigPath))
            {
                bindings.Add(new AppRuleTemplateBinding(
                    configPath: normalizedCurrentConfigPath,
                    templateId: activeTemplateId));
            }

            UserSettings updatedSettings = new UserSettings
            {
                Language = currentSettings.GetLanguage(),
                Mode = currentSettings.GetMode(),
                Protocol = currentSettings.GetProtocol(),
                IsSystemProxyUse = currentSettings.GetSystemProxyUsed(),
                IsUdpEnable = currentSettings.GetUdpEnabled(),
                IsRunningAtStartup = currentSettings.GetRunningAtStartupEnabled(),
                IsStartHidden = currentSettings.GetStartHiddenEnabled(),
                IsAutoConnect = currentSettings.GetAutoConnectEnabled(),
                IsSendingAnalytics = currentSettings.GetSendingAnalyticsEnabled(),
                ProxyPort = currentSettings.GetProxyPort(),
                TunPort = currentSettings.GetTunPort(),
                TestPort = currentSettings.GetTestPort(),
                TunIp = currentSettings.GetTunIp(),
                Dns = currentSettings.GetDns(),
                LogLevel = currentSettings.GetLogLevel(),
                LogPath = currentSettings.GetLogPath(),
                AppRulesMode = workingDefaultTemplate.Mode,
                AppRules = workingDefaultTemplate.AppRules.Select(rule => rule.Clone()).ToList(),
                AppRuleTemplates = templates,
                AppRuleTemplateBindings = bindings
            };

            onUpdateUserSettings?.Invoke(updatedSettings);
            DialogResult = true;
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private UserSettings RequireCurrentSettings()
        {
            if (getUserSettings == null)
                throw new InvalidOperationException("AppRulesWindow.Setup must be called before use.");

            return getUserSettings.Invoke();
        }

        private string GetTemplateDisplayName(AppRuleTemplate template)
        {
            if (IsDefaultTemplate(template.Id))
                return Localize("Lang.AppRules.Template.Default");

            return string.IsNullOrWhiteSpace(template.Name)
                ? Localize("Lang.AppRules.Template.Unnamed")
                : template.Name;
        }

        private string BuildNewTemplateName()
        {
            int index = workingCustomTemplates.Count + 1;
            return string.Format(Localize("Lang.AppRules.Template.NewName"), index);
        }

        private bool IsDefaultTemplate(string? templateId)
        {
            return string.IsNullOrWhiteSpace(templateId)
                || string.Equals(templateId, AppRuleTemplate.DefaultTemplateId, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeConfigPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                return System.IO.Path.GetFullPath(path.Trim());
            }
            catch
            {
                return path.Trim();
            }
        }

        private string Localize(string key)
        {
            return TryFindResource(key)?.ToString() ?? key;
        }
    }
}
