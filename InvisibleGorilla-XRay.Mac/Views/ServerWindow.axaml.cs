using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace InvisibleGorillaXRay.Mac.Views
{
    using Models;
    using Values;
    using InvisibleGorillaXRay.Services;
    using Utilities;
    using InvisibleGorillaXRay.Services.Analytics.ServerWindow;
    using InvisibleGorillaXRay.Services.Analytics.Configuration;

    public partial class ServerWindow : Window
    {
        private enum ImportingType { FILE, LINK }
        private enum SubscriptionOperation { CREATE, EDIT }

        private string configPath;
        private string groupPath;
        private ImportingType importingType;
        private SubscriptionOperation subscriptionOperation;

        private Func<string> getCurrentConfigPath;
        private Func<UserSettings> getUserSettings;
        private Func<AppRulesWindow> openAppRulesWindow;
        private Func<bool> isCurrentPathEqualRootConfigPath;
        private Func<List<Config>> getAllGeneralConfigs;
        private Func<string, List<Config>> getAllSubscriptionConfigs;
        private Func<List<Subscription>> getAllGroups;
        private Func<string, Status> convertLinkToConfig;
        private Func<string, string, Status> convertLinkToSubscription;
        private Func<string, Status> loadConfig;
        private Func<string, int> testConnection;
        private Func<string> getLogPath;
        private Action<string> onCopyConfig;
        private Action<string, string> onCreateConfig;
        private Action<string, string, string> onCreateSubscription;
        private Action<Subscription> onDeleteSubscription;
        private Action<GroupType, string> onDeleteConfig;
        private Action<string> onUpdateConfig;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();
        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public ServerWindow()
        {
            InitializeComponent();
            SetActiveFileImportingGroup(true);
            SetActiveLinkImportingGroup(false);
            importingType = ImportingType.FILE;

            Opened += OnWindowOpened;
        }

        public void Setup(
            Func<string> getCurrentConfigPath,
            Func<UserSettings> getUserSettings,
            Func<AppRulesWindow> openAppRulesWindow,
            Func<bool> isCurrentPathEqualRootConfigPath,
            Func<List<Config>> getAllGeneralConfigs,
            Func<string, List<Config>> getAllSubscriptionConfigs,
            Func<List<Subscription>> getAllGroups,
            Func<string, Status> convertLinkToConfig,
            Func<string, string, Status> convertLinkToSubscription,
            Func<string, Status> loadConfig,
            Func<string, int> testConnection,
            Func<string> getLogPath,
            Action<string> onCopyConfig,
            Action<string, string> onCreateConfig,
            Action<string, string, string> onCreateSubscription,
            Action<Subscription> onDeleteSubscription,
            Action<GroupType, string> onDeleteConfig,
            Action<string> onUpdateConfig)
        {
            this.getCurrentConfigPath = getCurrentConfigPath;
            this.getUserSettings = getUserSettings;
            this.openAppRulesWindow = openAppRulesWindow;
            this.isCurrentPathEqualRootConfigPath = isCurrentPathEqualRootConfigPath;
            this.getAllGeneralConfigs = getAllGeneralConfigs;
            this.getAllSubscriptionConfigs = getAllSubscriptionConfigs;
            this.getAllGroups = getAllGroups;
            this.convertLinkToConfig = convertLinkToConfig;
            this.convertLinkToSubscription = convertLinkToSubscription;
            this.loadConfig = loadConfig;
            this.testConnection = testConnection;
            this.getLogPath = getLogPath;
            this.onCopyConfig = onCopyConfig;
            this.onCreateConfig = onCreateConfig;
            this.onCreateSubscription = onCreateSubscription;
            this.onDeleteSubscription = onDeleteSubscription;
            this.onDeleteConfig = onDeleteConfig;
            this.onUpdateConfig = onUpdateConfig;
        }

        private void OnWindowOpened(object sender, EventArgs e)
        {
            groupPath = getCurrentConfigPath?.Invoke();
            LoadGroupsList();
            LoadConfigsList(GroupType.GENERAL);
            LoadConfigsList(GroupType.SUBSCRIPTION);
            ShowServersPanel();
            RefreshAppRulesSummary();

            if (isCurrentPathEqualRootConfigPath?.Invoke() == true)
                OnConfigTabClick(null, null);
            else
                OnSubscriptionTabClick(null, null);
        }

        private void OnConfigTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonConfigTab.IsEnabled = false;
            panelConfig.IsVisible = true;
        }

        private void OnSubscriptionTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();
            buttonSubscriptionTab.IsEnabled = false;
            panelSubscription.IsVisible = true;
        }

        private void OnAddConfigButtonClick(object sender, RoutedEventArgs e)
        {
            panelServers.IsVisible = false;
            panelAddConfigs.IsVisible = true;
            AnalyticsService.SendEvent(new AddConfigButtonClickedEvent());
        }

        private void OnAddSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            subscriptionOperation = SubscriptionOperation.CREATE;
            textBoxSubscriptionRemarks.Text = "";
            textBoxSubscriptionLink.Text = "";
            panelServers.IsVisible = false;
            panelAddSubscriptions.IsVisible = true;
            AnalyticsService.SendEvent(new AddSubButtonClickedEvent());
        }

        private void OnFileRadioButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveFileImportingGroup(true);
            SetActiveLinkImportingGroup(false);
            importingType = ImportingType.FILE;
        }

        private void OnLinkRadioButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveFileImportingGroup(false);
            SetActiveLinkImportingGroup(true);
            importingType = ImportingType.LINK;
        }

        private async void OnChooseFileButtonClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add configuration",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Config files") { Patterns = new[] { "*.json", "*.toml", "*.yaml", "*.yml" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count == 0) return;

            var file = files[0];
            textBlockFileName.Text = file.Name;
            configPath = file.Path.LocalPath;
        }

        private void OnImportConfigButtonClick(object sender, RoutedEventArgs e)
        {
            if (importingType == ImportingType.FILE)
            {
                if (string.IsNullOrEmpty(configPath))
                    return;

                panelLoading.IsVisible = true;
                TryAddConfigFromFile();
                AnalyticsService.SendEvent(new ConfigFromFileImportedEvent());
            }
            else
            {
                if (string.IsNullOrEmpty(textBoxConfigLink.Text))
                    return;

                panelLoading.IsVisible = true;
                TryAddConfigFromLink();
                AnalyticsService.SendEvent(new ConfigFromLinkImportedEvent());
            }
        }

        private void TryAddConfigFromFile()
        {
            Status configStatus = loadConfig.Invoke(configPath);
            if (configStatus.Code == Code.ERROR)
            {
                panelLoading.IsVisible = false;
                return;
            }

            onCopyConfig.Invoke(configPath);
            FinishAddConfig();
        }

        private void TryAddConfigFromLink()
        {
            Status configStatus = convertLinkToConfig.Invoke(textBoxConfigLink.Text);
            if (configStatus.Code == Code.ERROR)
            {
                panelLoading.IsVisible = false;
                return;
            }

            string[] config = (string[])configStatus.Content;
            onCreateConfig.Invoke(config[0], config[1]);
            FinishAddConfig();
        }

        private void FinishAddConfig()
        {
            string lastPath = GetLastConfigPath(GroupType.GENERAL);
            groupPath = lastPath;
            onUpdateConfig.Invoke(lastPath);
            panelLoading.IsVisible = false;
            LoadConfigsList(GroupType.GENERAL);
            configPath = null;
            textBlockFileName.Text = "No file chosen";
            textBoxConfigLink.Text = "";
            ShowServersPanel();
        }

        private void OnImportSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            if (subscriptionOperation == SubscriptionOperation.CREATE)
            {
                HandleImportingSubscription();
                AnalyticsService.SendEvent(new SubFromLinkImportedEvent());
            }
            else
            {
                HandleEditSubscription();
                AnalyticsService.SendEvent(new SubFromLinkEditedEvent());
            }
        }

        private void HandleImportingSubscription()
        {
            if (string.IsNullOrEmpty(textBoxSubscriptionRemarks.Text) ||
                string.IsNullOrEmpty(textBoxSubscriptionLink.Text))
                return;

            panelLoading.IsVisible = true;

            Status status = convertLinkToSubscription.Invoke(
                textBoxSubscriptionRemarks.Text,
                textBoxSubscriptionLink.Text);

            if (status.Code == Code.ERROR)
            {
                panelLoading.IsVisible = false;
                return;
            }

            string[] sub = (string[])status.Content;
            groupPath = "";
            onCreateSubscription.Invoke(sub[0], textBoxSubscriptionLink.Text, sub[1]);
            onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
            panelLoading.IsVisible = false;
            LoadGroupsList();
            LoadConfigsList(GroupType.SUBSCRIPTION);
            textBoxSubscriptionRemarks.Text = "";
            textBoxSubscriptionLink.Text = "";
            ShowServersPanel();
        }

        private void HandleEditSubscription()
        {
            var selected = comboBoxSubscription.SelectedItem;
            if (selected == null) return;

            var subscription = ((KeyValuePair<Subscription, string>)selected).Key;
            string oldRemarks = ((KeyValuePair<Subscription, string>)selected).Value;

            HandleImportingSubscription();

            if (oldRemarks != ((KeyValuePair<Subscription, string>)comboBoxSubscription.SelectedItem).Value)
            {
                onDeleteSubscription.Invoke(subscription);
                LoadGroupsList();
                LoadConfigsList(GroupType.SUBSCRIPTION);
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ShowServersPanel();
        }

        private void OnSubscriptionComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxSubscription.SelectedItem == null) return;
            var selected = (KeyValuePair<Subscription, string>)comboBoxSubscription.SelectedItem;
            groupPath = selected.Key.Directory.FullName;
            LoadConfigsList(GroupType.SUBSCRIPTION);
        }

        private void OnUpdateSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubUpdateButtonClickedEvent());
            if (comboBoxSubscription.SelectedItem == null) return;

            var selected = (KeyValuePair<Subscription, string>)comboBoxSubscription.SelectedItem;
            textBoxSubscriptionRemarks.Text = selected.Value;
            textBoxSubscriptionLink.Text = selected.Key.Url;

            HandleEditSubscription();
        }

        private void OnEditSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubEditButtonClickedEvent());
            if (comboBoxSubscription.SelectedItem == null) return;

            subscriptionOperation = SubscriptionOperation.EDIT;
            var selected = (KeyValuePair<Subscription, string>)comboBoxSubscription.SelectedItem;
            textBoxSubscriptionRemarks.Text = selected.Value;
            textBoxSubscriptionLink.Text = selected.Key.Url;

            panelServers.IsVisible = false;
            panelAddSubscriptions.IsVisible = true;
        }

        private void OnDeleteSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            AnalyticsService.SendEvent(new SubDeleteButtonClickedEvent());
            if (comboBoxSubscription.SelectedItem == null) return;

            var selected = (KeyValuePair<Subscription, string>)comboBoxSubscription.SelectedItem;
            onDeleteSubscription.Invoke(selected.Key);
            LoadGroupsList();
            LoadConfigsList(GroupType.SUBSCRIPTION);
            onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
        }

        private void ShowServersPanel()
        {
            panelAddConfigs.IsVisible = false;
            panelAddSubscriptions.IsVisible = false;
            panelServers.IsVisible = true;
            RefreshAppRulesSummary();
        }

        private void LoadConfigsList(GroupType group)
        {
            if (group == GroupType.GENERAL)
                LoadGeneralConfigsList();
            else
                LoadSubscriptionConfigsList();

            RefreshAppRulesSummary();
        }

        private void LoadGeneralConfigsList()
        {
            List<Config> configs = getAllGeneralConfigs.Invoke();
            listConfigs.Children.Clear();

            textNoConfig.IsVisible = configs == null || configs.Count == 0;

            if (configs == null) return;

            foreach (Config config in configs)
            {
                var item = CreateConfigItem(config, GroupType.GENERAL);
                listConfigs.Children.Add(item);
            }
        }

        private void LoadSubscriptionConfigsList()
        {
            List<Config> configs = getAllSubscriptionConfigs?.Invoke(groupPath);
            List<Subscription> groups = getAllGroups?.Invoke() ?? new List<Subscription>();
            listSubscriptions.Children.Clear();

            panelSubscriptionControl.IsVisible = groups.Count > 0;
            textNoSubscription.IsVisible = (configs == null || configs.Count == 0) && groups.Count == 0;

            if (configs == null) return;

            foreach (Config config in configs)
            {
                var item = CreateConfigItem(config, GroupType.SUBSCRIPTION);
                listSubscriptions.Children.Add(item);
            }
        }

        private Border CreateConfigItem(Config config, GroupType group)
        {
            string currentPath = getCurrentConfigPath?.Invoke();
            bool isSelected = config.Path == currentPath;

            var border = new Border
            {
                Background = isSelected
                    ? Avalonia.Media.Brush.Parse("#3d3d3d")
                    : Avalonia.Media.Brushes.Transparent,
                Padding = new Avalonia.Thickness(10, 6),
                Cursor = new Avalonia.Input.Cursor(StandardCursorType.Hand)
            };

            var outerStack = new StackPanel { Spacing = 2 };

            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            topRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var nameBlock = new TextBlock
            {
                Text = config.Name,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameBlock, 0);

            var statusBlock = new TextBlock
            {
                Text = isSelected ? "●" : "",
                FontSize = 11,
                Foreground = isSelected
                    ? Avalonia.Media.Brush.Parse("#43b581")
                    : Avalonia.Media.Brushes.Gray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(6, 0)
            };
            Grid.SetColumn(statusBlock, 1);

            topRow.Children.Add(nameBlock);
            topRow.Children.Add(statusBlock);

            var btnRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
                Margin = new Avalonia.Thickness(0, 2, 0, 0)
            };

            Button MakeBtn(string label, string fg = "#aaa")
            {
                return new Button
                {
                    Content = label,
                    FontSize = 11,
                    Foreground = Avalonia.Media.Brush.Parse(fg),
                    Background = Avalonia.Media.Brush.Parse("#2a2a2a"),
                    BorderBrush = Avalonia.Media.Brush.Parse("#444"),
                    BorderThickness = new Avalonia.Thickness(1),
                    Padding = new Avalonia.Thickness(8, 2),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Cursor = new Avalonia.Input.Cursor(StandardCursorType.Hand)
                };
            }

            var selectBtn = MakeBtn(LocalizationService.GetTerm("Lang.Config.Select"), "#43b581");
            selectBtn.Click += (s, e) =>
            {
                onUpdateConfig.Invoke(config.Path);
                LoadConfigsList(group);
                AnalyticsService.SendEvent(new SelectButtonClickedEvent());
            };
            btnRow.Children.Add(selectBtn);

            var checkBtn = MakeBtn(LocalizationService.GetTerm("Lang.Config.Check"));
            checkBtn.Click += (s, e) =>
            {
                statusBlock.Text = "...";
                statusBlock.Foreground = Avalonia.Media.Brushes.Gray;
                checkBtn.IsEnabled = false;
                Task.Run(() =>
                {
                    try
                    {
                        Status configStatus = loadConfig.Invoke(config.Path);
                        if (configStatus.Code == Code.ERROR)
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                checkBtn.IsEnabled = true;
                                statusBlock.Text = "error";
                                statusBlock.Foreground = Avalonia.Media.Brush.Parse("#f04747");
                            });
                            return;
                        }

                        int ping = testConnection.Invoke(configStatus.Content.ToString());
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            checkBtn.IsEnabled = true;
                            if (ping >= 0)
                            {
                                statusBlock.Text = $"{ping}ms";
                                statusBlock.Foreground = ping < 300
                                    ? Avalonia.Media.Brush.Parse("#43b581")
                                    : Avalonia.Media.Brush.Parse("#faa61a");
                            }
                            else
                            {
                                statusBlock.Text = "timeout";
                                statusBlock.Foreground = Avalonia.Media.Brush.Parse("#f04747");
                            }
                        });
                    }
                    catch
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            checkBtn.IsEnabled = true;
                            statusBlock.Text = "error";
                            statusBlock.Foreground = Avalonia.Media.Brush.Parse("#f04747");
                        });
                    }
                });
                AnalyticsService.SendEvent(new CheckButtonClickedEvent());
            };
            btnRow.Children.Add(checkBtn);

            var shareBtn = MakeBtn(LocalizationService.GetTerm("Lang.Config.Share"));
            shareBtn.Click += (s, e) =>
            {
                try
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    clipboard?.SetTextAsync(config.Path);
                    shareBtn.Content = "✓";
                    Task.Delay(1500).ContinueWith(_ =>
                        Dispatcher.UIThread.InvokeAsync(() =>
                            shareBtn.Content = LocalizationService.GetTerm("Lang.Config.Share")));
                }
                catch { }
                AnalyticsService.SendEvent(new ShareButtonClickedEvent());
            };
            btnRow.Children.Add(shareBtn);

            var logBtn = MakeBtn(LocalizationService.GetTerm("Lang.Config.Log"));
            logBtn.Click += (s, e) =>
            {
                try
                {
                    string logPath = getLogPath?.Invoke();
                    if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
                        Process.Start("open", logPath);
                }
                catch { }
                AnalyticsService.SendEvent(new LogButtonClickedEvent());
            };
            btnRow.Children.Add(logBtn);

            var deleteBtn = MakeBtn(LocalizationService.GetTerm("Lang.Config.Delete"), "#f04747");
            deleteBtn.Click += (s, e) =>
            {
                DeleteConfig(config, group);
                AnalyticsService.SendEvent(new DeleteButtonClickedEvent());
            };
            btnRow.Children.Add(deleteBtn);

            outerStack.Children.Add(topRow);
            outerStack.Children.Add(btnRow);

            border.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed &&
                    e.Source is not Button)
                {
                    onUpdateConfig.Invoke(config.Path);
                    LoadConfigsList(group);
                    AnalyticsService.SendEvent(new SelectButtonClickedEvent());
                }
            };

            border.Child = outerStack;

            return border;
        }

        private void DeleteConfig(Config config, GroupType group)
        {
            onDeleteConfig.Invoke(group, config.Path);
            groupPath = config.Path;
            if (group == GroupType.SUBSCRIPTION)
            {
                List<Config> remaining = getAllSubscriptionConfigs.Invoke(groupPath);
                if (remaining == null || remaining.Count == 0)
                    LoadGroupsList();
            }
            LoadConfigsList(group);
            if (getCurrentConfigPath.Invoke() == config.Path)
                onUpdateConfig.Invoke(GetLastConfigPath(group));
        }

        private void LoadGroupsList()
        {
            var groups = getAllGroups?.Invoke() ?? new List<Subscription>();
            var dict = new Dictionary<Subscription, string>();
            foreach (var group in groups)
                dict.Add(group, group.Directory.Name);

            comboBoxSubscription.ItemsSource = dict.ToList();
            comboBoxSubscription.DisplayMemberBinding = new Avalonia.Data.Binding("Value");
            comboBoxSubscription.SelectedValueBinding = new Avalonia.Data.Binding("Key");

            if (dict.Count > 0)
            {
                var current = dict.FirstOrDefault(g =>
                    g.Key.Directory.FullName == FileUtility.GetDirectory(groupPath));
                if (current.Key == null)
                    comboBoxSubscription.SelectedIndex = dict.Count - 1;
                else
                    comboBoxSubscription.SelectedItem = current;
            }
        }

        private string GetLastConfigPath(GroupType group)
        {
            if (group == GroupType.GENERAL)
            {
                var configs = getAllGeneralConfigs?.Invoke();
                if (configs != null && configs.Count > 0) return configs.Last().Path;
                var subConfigs = getAllSubscriptionConfigs?.Invoke(groupPath);
                if (subConfigs != null && subConfigs.Count > 0) return subConfigs.Last().Path;
            }
            else
            {
                var configs = getAllSubscriptionConfigs?.Invoke(groupPath);
                if (configs != null && configs.Count > 0) return configs.Last().Path;
                var genConfigs = getAllGeneralConfigs?.Invoke();
                if (genConfigs != null && genConfigs.Count > 0) return genConfigs.Last().Path;
            }
            return null;
        }

        private void SetActiveFileImportingGroup(bool isActive)
        {
            textBoxConfigLink.Text = "";
            buttonConfigFile.IsEnabled = isActive;
        }

        private void SetActiveLinkImportingGroup(bool isActive)
        {
            configPath = null;
            textBlockFileName.Text = "No file chosen";
            textBoxConfigLink.IsEnabled = isActive;
        }

        private void HideAllPanels()
        {
            panelConfig.IsVisible = false;
            panelSubscription.IsVisible = false;
        }

        private void EnableAllTabs()
        {
            buttonConfigTab.IsEnabled = true;
            buttonSubscriptionTab.IsEnabled = true;
        }

        private async void OnManageAppRulesClick(object? sender, RoutedEventArgs e)
        {
            AppRulesWindow appRulesWindow = openAppRulesWindow.Invoke();
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

        private string Localize(string key) => LocalizationService.GetTerm(key);
    }
}
