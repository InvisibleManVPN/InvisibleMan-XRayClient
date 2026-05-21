using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Generic;
using Microsoft.Win32;
using QRCoder;
using QRCoder.Xaml;

namespace InvisibleGorillaXRay
{
    using Models;
    using Values;
    using Services;

    public partial class ServerWindow : Window
    {
        private Action pendingToRenderActions = delegate { };

        private Func<string> getCurrentConfigPath;
        private Func<UserSettings> getUserSettings;
        private Func<AppRulesWindow> openAppRulesWindow;
        private Func<bool> isCurrentPathEqualRootConfigPath;
        private Func<string, int> testConnection;
        private Func<string> getLogPath;
        private string pendingShareContent;
        private string pendingShareConfigName;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public ServerWindow()
        {
            InitializeComponent();
            InitializeImportingGroups();
            InitializeGeneralConfigComponents();
            InitializeSubscriptionConfigComponents();
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

        protected override void OnContentRendered(EventArgs e)
        {
            InitializeGroupPath();
            LoadGroupsList();
            LoadConfigsLists();
            ShowServersPanel();
            HandleShowingActiveTab();
            ExecutePendingToRenderActions();
            
            void LoadConfigsLists()
            {
                LoadConfigsList(GroupType.GENERAL);
                LoadConfigsList(GroupType.SUBSCRIPTION);
            }

            void HandleShowingActiveTab()
            {
                HideAllPanels();

                if (isCurrentPathEqualRootConfigPath.Invoke())
                    OnConfigTabClick(null, null);
                else
                    OnSubscriptionTabClick(null, null);
            }

            void ExecutePendingToRenderActions() => pendingToRenderActions.Invoke();

            RefreshAppRulesSummary();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ShowServersPanel();
        }

        private void OnBackShareButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveSharePanel(false);
        }

        private void OnShareQrButtonClick(object sender, RoutedEventArgs e)
        {
            if (!HasPendingShareContent())
                return;

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(
                plainText: pendingShareContent,
                eccLevel: QRCodeGenerator.ECCLevel.Default
            );

            XamlQRCode qrCode = new XamlQRCode(qrCodeData);
            DrawingImage qrCodeAsXaml = qrCode.GetGraphic(20);
            imageQrCode.Source = qrCodeAsXaml;
            imageQrCode.Visibility = Visibility.Visible;
        }

        private void OnShareFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (!HasPendingShareContent())
                return;

            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Title = GetResourceText("Lang.Share.ConfigFile"),
                FileName = GetPendingShareFileName(),
                Filter = "Config files|*.json|All files|*.*",
                AddExtension = true,
                DefaultExt = ".json"
            };

            if (fileDialog.ShowDialog(this) != true)
                return;

            try
            {
                File.WriteAllText(fileDialog.FileName, pendingShareContent, Encoding.UTF8);
                MessageBox.Show(
                    this,
                    GetResourceText("Lang.Share.FileSaved"),
                    Values.Caption.INFO,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Values.Caption.ERROR, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnShareLinkButtonClick(object sender, RoutedEventArgs e)
        {
            if (!HasPendingShareContent())
                return;

            try
            {
                string shareLink = BuildShareLink(pendingShareContent, pendingShareConfigName);
                Clipboard.SetText(shareLink);
                textBoxShareLink.Text = shareLink;
                textBoxShareLink.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Values.Caption.ERROR, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                this,
                GetResourceText("Lang.Share.LinkCopied"),
                Values.Caption.INFO,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ShowServersPanel()
        {
            panelAddConfigs.Visibility = Visibility.Hidden;
            panelAddSubscriptions.Visibility = Visibility.Hidden;
            panelServers.Visibility = Visibility.Visible;
            SelectConfig(getCurrentConfigPath.Invoke());
            RefreshAppRulesSummary();
        }

        private void SetImportingType(ImportingType type) => importingType = type;

        private void LoadConfigsList(GroupType group)
        {
            if (group == GroupType.GENERAL)
                LoadGeneralConfigsList();
            else
                LoadSubscriptionConfigsList();
        }

        private void ClearConfigsList(StackPanel list)
        {
            list.Children.Clear();
        }

        private void HandleShowingNoServerExistsHint(
            List<Config> configs, 
            List<Subscription> groups, 
            TextBlock textNoServer
        )
        {
            if (groups.Count == 0 && configs.Count == 0)
                textNoServer.Visibility = Visibility.Visible;
            else
                textNoServer.Visibility = Visibility.Collapsed;
        }

        private void AddAllConfigsToList(
            List<Config> configs,
            List<Components.Config> configComponents,
            StackPanel parent
        )
        {
            foreach (Config config in configs)
            {
                Components.Config configComponent = CreateConfigComponent(config);
                AddConfigToList(configComponent, configComponents, parent);
            }

            Components.Config CreateConfigComponent(Config config)
            {
                Components.Config configComponent = new Components.Config();
                configComponent.Setup(
                    config: config, 
                    onSelect: () => {
                        onUpdateConfig.Invoke(config.Path);
                        SelectConfig(config.Path);
                    },
                    onDelete: () => {
                        onDeleteConfig.Invoke(config.Group, config.Path);
                        groupPath = config.Path;
                        HandleReloadingGroupsList();
                        LoadConfigsList(config.Group);
                        HandleCurrentConfigPath();
                        SelectConfig(getCurrentConfigPath.Invoke());

                        void HandleReloadingGroupsList()
                        {
                            if (config.Group == GroupType.SUBSCRIPTION && configs.Count == 1)
                                LoadGroupsList();
                        }
                        
                        void HandleCurrentConfigPath()
                        {
                            if(IsCurrentConfigDeleted())
                                onUpdateConfig.Invoke(GetLastConfigPath(config.Group));
                            
                            bool IsCurrentConfigDeleted() => getCurrentConfigPath.Invoke() == config.Path;
                        }
                    },
                    onShare: (content) => {
                        pendingShareContent = content;
                        pendingShareConfigName = config.Name;
                        imageQrCode.Source = null;
                        imageQrCode.Visibility = Visibility.Collapsed;
                        textBoxShareLink.Text = null;
                        textBoxShareLink.Visibility = Visibility.Collapsed;
                        SetActiveSharePanel(true);
                    },
                    getServerWindow: () => this,
                    testConnection: (configPath) => {
                        Status configStatus = loadConfig.Invoke(configPath);
                        if (configStatus.Code == Code.ERROR)
                            return Availability.ERROR;
                            
                        return testConnection.Invoke(configStatus.Content.ToString());
                    },
                    getLogPath: getLogPath
                );

                return configComponent;
            }

            void AddConfigToList(
                Components.Config config, 
                List<Components.Config> configComponentsList, 
                StackPanel parent
            )
            {
                configComponentsList.Add(config);
                parent.Children.Add(config);
            }
        }

        private bool IsAnyConfigExists(List<Config> configs)
        {
            return configs != null && configs.Count > 0;
        }

        private bool HasPendingShareContent()
        {
            return !string.IsNullOrWhiteSpace(pendingShareContent);
        }

        private string BuildShareLink(string content, string configName)
        {
            string configNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(configName);
            string metadataName = Uri.EscapeDataString(
                string.IsNullOrWhiteSpace(configNameWithoutExtension)
                    ? "config"
                    : configNameWithoutExtension);
            string base64Config = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
            string dataConfigLink = $"data:application/json;name={metadataName};base64,{base64Config}";

            return $"{DeepLink.CONFIG_DATA}{Uri.EscapeDataString(dataConfigLink)}";
        }

        private string GetPendingShareFileName()
        {
            string fileName = System.IO.Path.GetFileName(pendingShareConfigName);
            if (string.IsNullOrWhiteSpace(fileName))
                return "config.json";

            return System.IO.Path.HasExtension(fileName) ? fileName : $"{fileName}.json";
        }

        private string GetResourceText(string key)
        {
            return TryFindResource(key)?.ToString() ?? key;
        }

        private void AddConfigHintAtTheEndOfList(StackPanel list)
        {
            list.Children.Add(new Components.ConfigHint());
        }

        private string GetLastConfigPath(GroupType group)
        {
            if (group == GroupType.GENERAL)
                return GetLastGeneralConfigPath();
            else
                return GetLastSubscriptionConfigPath();
        } 

        private void SelectConfig(string path)
        {
            DeselectAllConfigs();
            SelectConfig();
            RefreshAppRulesSummary();

            void DeselectAllConfigs()
            {
                DeselectAllConfigsForComponentsList(generalConfigComponents);
                DeselectAllConfigsForComponentsList(subscriptionConfigComponents);

                void DeselectAllConfigsForComponentsList(List<Components.Config> configComponents)
                {
                    configComponents.ForEach(
                        configComponent => configComponent.SetSelection(false)
                    );
                }
            }
            
            void SelectConfig()
            {
                Components.Config configComponent = FindConfigInComponentsList(generalConfigComponents);

                if (!IsAnyConfigExists())
                {
                    configComponent = FindConfigInComponentsList(subscriptionConfigComponents);
                    if (!IsAnyConfigExists())
                        return;
                }

                configComponent.SetSelection(true);

                Components.Config FindConfigInComponentsList(List<Components.Config> configComponents)
                {
                    return configComponents.Find(
                        component => component.GetConfig().Path == path
                    );
                }

                bool IsAnyConfigExists() => configComponent != null;
            } 
        }

        private void DeleteSubscription(Subscription subscription)
        {
            onDeleteSubscription.Invoke(subscription);
            LoadGroupsList();
            LoadConfigsList(GroupType.SUBSCRIPTION);
            onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
            SelectConfig(getCurrentConfigPath.Invoke());
        }

        private void SetActiveSharePanel(bool isActive)
        {
            if (!isActive)
            {
                pendingShareContent = null;
                pendingShareConfigName = null;
                imageQrCode.Source = null;
                imageQrCode.Visibility = Visibility.Collapsed;
                textBoxShareLink.Text = null;
                textBoxShareLink.Visibility = Visibility.Collapsed;
            }

            SetActivePanel(panelShare, isActive);
        }

        private void SetActiveLoadingPanel(bool isActive) => SetActivePanel(panelLoading, isActive);

        private void SetActivePanel(Panel panel, bool isActive)
        {
            panel.Visibility = isActive ? Visibility.Visible : Visibility.Hidden;
        }
        
        private void HideAllPanels()
        {
            SetActiveConfigPanel(false);
            SetActiveSubscriptionPanel(false);
        }

        private void SetEnableButton(Button button, bool isEnabled)
        {
            button.IsEnabled = isEnabled;
        }

        private void EnableAllTabs()
        {
            SetEnableConfigTabButton(true);
            SetEnableSubscriptionTabButton(true);
        }

        private void OnManageAppRulesClick(object sender, RoutedEventArgs e)
        {
            AppRulesWindow appRulesWindow = openAppRulesWindow.Invoke();
            appRulesWindow.Owner = this;
            appRulesWindow.ShowDialog();
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

        private string Localize(string key)
        {
            return TryFindResource(key)?.ToString() ?? key;
        }

        private void HandleWarningMessage(string message)
        {
            MessageBoxResult result = MessageBox.Show(
                this,
                message, 
                Caption.WARNING, 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning
            );
        }

        private void HandleErrorMessage(string message)
        {
            MessageBox.Show(
                this,
                message, 
                Caption.ERROR, 
                MessageBoxButton.OK, 
                MessageBoxImage.Error
            );
        }
    }
}
