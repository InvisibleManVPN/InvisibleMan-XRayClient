using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace InvisibleGorillaXRay.Mac.Views
{
    using Models;
    using InvisibleGorillaXRay.Mac.Services;

    public partial class AppRulesWindow : Window
    {
        private sealed class TemplateComboItem
        {
            public string Id { get; init; } = string.Empty;
            public string Name { get; init; } = string.Empty;

            public override string ToString() => Name;
        }

        private Func<UserSettings>? getUserSettings;
        private Action<UserSettings>? onUpdateUserSettings;
        private readonly List<MacInstalledAppInfo> discoveredMacApps = new();
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

            discoveredMacApps.Clear();
            discoveredMacApps.AddRange(MacInstalledAppDiscovery.GetApps());

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

            TemplateComboItem selectedItem = items.FirstOrDefault(item =>
                    string.Equals(item.Id, preferredTemplateId, StringComparison.OrdinalIgnoreCase))
                ?? items[0];

            isApplyingTemplate = true;
            comboBoxTemplate.SelectedItem = selectedItem;
            activeTemplateId = selectedItem.Id;
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
            HashSet<string> selectedIds = template.AppRules
                .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.AppId))
                .Select(rule => rule.AppId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IEnumerable<MacInstalledAppInfo> filteredApps = discoveredMacApps;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filteredApps = filteredApps.Where(app =>
                    app.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || app.AppId.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (MacInstalledAppInfo app in filteredApps)
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

                panelAppItems.Children.Add(card);
            }

            textBlockNoApps.IsVisible = panelAppItems.Children.Count == 0;
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

            List<AppRule> rules = discoveredMacApps
                .Where(app => selectedIds.Contains(app.AppId))
                .Select(app => new AppRule(
                    appId: app.AppId,
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

        private void OnTemplateSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
            activeTemplateId = (comboBoxTemplate.SelectedItem as TemplateComboItem)?.Id
                ?? AppRuleTemplate.DefaultTemplateId;
            ApplyTemplateToEditor(GetTemplateById(activeTemplateId));
        }

        private void OnTemplateNameChanged(object? sender, TextChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate || IsDefaultTemplate(activeTemplateId))
                return;

            CaptureActiveTemplateState();
            GetTemplateById(activeTemplateId).Name = textBoxTemplateName.Text?.Trim() ?? string.Empty;
            PopulateTemplateSelector(activeTemplateId);
        }

        private void OnModeChanged(object? sender, RoutedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (!isReady || isApplyingTemplate)
                return;

            CaptureActiveTemplateState();
            RenderDiscoveredApps(GetTemplateById(activeTemplateId));
        }

        private void OnRefreshAppsClick(object? sender, RoutedEventArgs e)
        {
            CaptureActiveTemplateState();

            discoveredMacApps.Clear();
            discoveredMacApps.AddRange(MacInstalledAppDiscovery.GetApps());
            RenderDiscoveredApps(GetTemplateById(activeTemplateId));
        }

        private void OnNewTemplateClick(object? sender, RoutedEventArgs e)
        {
            CaptureActiveTemplateState();

            AppRuleTemplate source = GetTemplateById(activeTemplateId).Clone();
            source.Id = Guid.NewGuid().ToString("N");
            source.Name = BuildNewTemplateName();
            workingCustomTemplates.Add(source);

            PopulateTemplateSelector(source.Id);
        }

        private void OnDeleteTemplateClick(object? sender, RoutedEventArgs e)
        {
            if (IsDefaultTemplate(activeTemplateId))
                return;

            workingCustomTemplates.RemoveAll(template =>
                string.Equals(template.Id, activeTemplateId, StringComparison.OrdinalIgnoreCase));
            activeTemplateId = AppRuleTemplate.DefaultTemplateId;
            PopulateTemplateSelector(activeTemplateId);
        }

        private void OnSaveButtonClick(object? sender, RoutedEventArgs e)
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
            Close();
        }

        private void OnCancelButtonClick(object? sender, RoutedEventArgs e)
        {
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
            if (Application.Current?.TryFindResource(key, out object? value) == true)
                return value?.ToString() ?? key;

            return key;
        }
    }
}
