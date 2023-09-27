using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Microsoft.Win32;

namespace InvisibleManXRay
{
    using Models;
    using Values;
    using Services;
    using Services.Analytics.ServerWindow;
    using Utilities;

    public partial class ServerWindow : Window
    {
        private enum ImportingType { FILE, LINK }

        private string configPath = null;
        private string groupPath = null;
        private ImportingType importingType;

        private List<Components.Config> generalConfigComponents;
        private List<Components.Config> subscriptionConfigComponents;

        private Func<string> getCurrentConfigPath;
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

        public ServerWindow()
        {
            InitializeComponent();
            InitializeImportingGroups();
            InitializeConfigComponents();

            void InitializeImportingGroups()
            {
                SetActiveFileImportingGroup(true);
                SetActiveLinkImportingGroup(false);
                SetImportingType(ImportingType.FILE);
            }

            void InitializeConfigComponents()
            {
                generalConfigComponents = new List<Components.Config>();
                subscriptionConfigComponents = new List<Components.Config>();
            }
        }

        public void Setup(
            Func<string> getCurrentConfigPath,
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

            void InitializeGroupPath()
            {
                groupPath = getCurrentConfigPath.Invoke();
            } 
            
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
        }

        private void OnConfigTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableConfigTabButton(false);
            SetActiveConfigPanel(true);
        }

        private void OnSubscriptionTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableSubscriptionTabButton(false);
            SetActiveSubscriptionPanel(true);
        }

        private void OnSubscriptionComboBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            if(comboBoxSubscription.SelectedValue == null)
                return;

            groupPath = ((Subscription)comboBoxSubscription.SelectedValue).Directory.FullName;
            LoadConfigsList(GroupType.SUBSCRIPTION);
            SelectConfig(getCurrentConfigPath.Invoke());
        }

        private void OnDeleteSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            if(comboBoxSubscription.SelectedValue == null)
                return;
            
            string text = comboBoxSubscription.Text;
            Subscription subscription = (Subscription)comboBoxSubscription.SelectedValue;

            MessageBoxResult result = MessageBox.Show(
                this,
                string.Format(Message.DELETE_CONFIRMATION, text),
                Caption.INFO,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                onDeleteSubscription.Invoke(subscription);
                LoadGroupsList();
                LoadConfigsList(GroupType.SUBSCRIPTION);
                onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
                SelectConfig(getCurrentConfigPath.Invoke());
            }
        }

        private void OnAddConfigButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddConfigsServerPanel();
            AnalyticsService.SendEvent(new AddConfigButtonClickedEvent());
        }

        private void OnAddSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddSubscriptionsServerPanel();
            AnalyticsService.SendEvent(new AddSubButtonClickedEvent());
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ShowServersPanel();
        }

        private void OnFileRadioButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveFileImportingGroup(true);
            SetActiveLinkImportingGroup(false);
            SetImportingType(ImportingType.FILE);
        }

        private void OnLinkRadioButtonClick(object sender, RoutedEventArgs e)
        {
            SetActiveFileImportingGroup(false);
            SetActiveLinkImportingGroup(true);
            SetImportingType(ImportingType.LINK);
        }
       
        private void OnChooseFileButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = InitializeFileDialog();
            bool? result = fileDialog.ShowDialog();

            if (!IsFileSelected())
                return;

            textBlockFileName.Text = GetFileName();
            configPath = GetFilePath();

            OpenFileDialog InitializeFileDialog()
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "Add configuration";
                fileDialog.Filter = "Config files|*.json;*.toml;*.yaml;*.yml|All files|*.*";

                return fileDialog;
            }

            bool IsFileSelected() => result == true;

            string GetFileName() => System.IO.Path.GetFileName(fileDialog.FileName);

            string GetFilePath() => fileDialog.FileName;
        }

        private void OnImportConfigButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsFileImporting())
            {
                HandleImportingConfigFromFile();
                AnalyticsService.SendEvent(new ConfigFromFileImportedEvent());
            }
            else
            {
                HandleImportingConfigFromLink();
                AnalyticsService.SendEvent(new ConfigFromLinkImportedEvent());
            }

            bool IsFileImporting() => importingType == ImportingType.FILE;

            void HandleImportingConfigFromFile()
            {
                if (!IsFileSelected())
                {
                    MessageBox.Show(
                        this,
                        Values.Message.NO_CONFIG_FILE_SELECTED, 
                        Values.Caption.WARNING, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning
                    );
                    return;
                }

                SetActiveLoadingPanel(true);
                TryAddConfig(ConfigType.FILE);

                bool IsFileSelected() => !string.IsNullOrEmpty(configPath);
            }

            void HandleImportingConfigFromLink()
            {
                if (!IsLinkEntered())
                {
                    MessageBox.Show(
                        this,
                        Values.Message.NO_CONFIG_LINK_ENTERED, 
                        Values.Caption.WARNING, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning
                    );
                    return;
                }

                SetActiveLoadingPanel(true);
                TryAddConfig(ConfigType.URL);

                bool IsLinkEntered() => !string.IsNullOrEmpty(textBoxConfigLink.Text);
            }

            void TryAddConfig(ConfigType type)
            {
                Status configStatus;

                if (type == ConfigType.FILE)
                {
                    configStatus = loadConfig.Invoke(configPath);
                    if (configStatus.Code == Code.ERROR)
                    {
                        HandleError();
                        SetActiveLoadingPanel(false);
                        return;
                    }

                    onCopyConfig.Invoke(configPath);
                }
                else
                {
                    configStatus = convertLinkToConfig.Invoke(textBoxConfigLink.Text);
                    if (configStatus.Code == Code.ERROR)
                    {
                        HandleError();
                        SetActiveLoadingPanel(false);
                        return;
                    }

                    string[] config = GetConfig();
                    onCreateConfig.Invoke(GetConfigRemark(), GetConfigData());

                    string[] GetConfig() => (string[])configStatus.Content;

                    string GetConfigRemark() => config[0];

                    string GetConfigData() => config[1];
                }
                
                groupPath = GetLastConfigPath(GroupType.GENERAL);
                onUpdateConfig.Invoke(GetLastConfigPath(GroupType.GENERAL));
                SetActiveLoadingPanel(false);
                LoadConfigsList(GroupType.GENERAL);
                ClearConfigPath();
                ClearConfigLink();
                ShowServersPanel();

                void HandleError()
                {
                    switch (configStatus.SubCode)
                    {
                        case SubCode.NO_CONFIG:
                        case SubCode.UNSUPPORTED_LINK:
                            HandleWarningMessage(configStatus.Content.ToString());
                            break;
                        case SubCode.INVALID_CONFIG:
                            HandleErrorMessage(configStatus.Content.ToString());
                            break;
                        default:
                            return;
                    }
                }
            }
        }

        private void OnImportSubscriptionButtonClick(object sender, RoutedEventArgs e)
        {
            HandleImportingSubscription();

            void HandleImportingSubscription()
            {
                if (!IsRemarksEntered())
                {
                    MessageBox.Show(
                        this,
                        Values.Message.NO_SUBSCRIPTION_REMARKS_ENTERED, 
                        Values.Caption.WARNING, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning
                    );
                    return;
                }
                else if (!IsLinkEntered())
                {
                    MessageBox.Show(
                        this,
                        Values.Message.NO_SUBSCRIPTION_LINK_ENTERED, 
                        Values.Caption.WARNING, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning
                    );
                    return;
                }

                SetActiveLoadingPanel(true);
                TryAddSubscription();

                bool IsRemarksEntered() => !string.IsNullOrEmpty(textBoxSubscriptionRemarks.Text);

                bool IsLinkEntered() => !string.IsNullOrEmpty(textBoxSubscriptionLink.Text);
            }

            void TryAddSubscription()
            {
                Status subscriptionStatus;

                subscriptionStatus = convertLinkToSubscription.Invoke(
                    textBoxSubscriptionRemarks.Text, 
                    textBoxSubscriptionLink.Text
                );

                if (subscriptionStatus.Code == Code.ERROR)
                {
                    HandleError();
                    SetActiveLoadingPanel(false);
                    return;
                }

                string[] subscription = GetSubscription();
                groupPath = "";
                onCreateSubscription.Invoke(
                    GetSubscriptionRemark(), 
                    GetSubscriptionUrl(), 
                    GetSubscriptionData()
                );
                onUpdateConfig.Invoke(GetLastConfigPath(GroupType.SUBSCRIPTION));
                SetActiveLoadingPanel(false);
                LoadGroupsList();
                LoadConfigsList(GroupType.SUBSCRIPTION);
                ClearSubscriptionRemarks();
                ClearSubscriptionPath();
                ShowServersPanel();

                string[] GetSubscription() => (string[])subscriptionStatus.Content;

                string GetSubscriptionUrl() => textBoxSubscriptionLink.Text;

                string GetSubscriptionRemark() => subscription[0];

                string GetSubscriptionData() => subscription[1];

                void HandleError()
                {
                    switch (subscriptionStatus.SubCode)
                    {
                        case SubCode.NO_CONFIG:
                        case SubCode.UNSUPPORTED_LINK:
                            HandleWarningMessage(subscriptionStatus.Content.ToString());
                            break;
                        case SubCode.INVALID_CONFIG:
                            HandleErrorMessage(subscriptionStatus.Content.ToString());
                            break;
                        default:
                            return;
                    }
                }
            }
        }

        private void ShowAddConfigsServerPanel()
        {
            panelServers.Visibility = Visibility.Hidden;
            panelAddConfigs.Visibility = Visibility.Visible;
        }

        private void ShowAddSubscriptionsServerPanel()
        {
            panelServers.Visibility = Visibility.Hidden;
            panelAddSubscriptions.Visibility = Visibility.Visible;
        }

        private void ShowServersPanel()
        {
            panelAddConfigs.Visibility = Visibility.Hidden;
            panelAddSubscriptions.Visibility = Visibility.Hidden;
            panelServers.Visibility = Visibility.Visible;
            SelectConfig(getCurrentConfigPath.Invoke());
        }

        private void SetActiveFileImportingGroup(bool isActive)
        {
            ClearConfigLink();
            buttonConfigFile.IsEnabled = isActive;
        }

        private void SetActiveLinkImportingGroup(bool isActive)
        {
            ClearConfigPath();
            textBoxConfigLink.IsEnabled = isActive;
        }

        private void SetImportingType(ImportingType type) => importingType = type;

        private void ClearConfigPath()
        {
            configPath = null;
            textBlockFileName.Text = "No file chosen...";
        }

        private void ClearConfigLink()
        {
            textBoxConfigLink.Text = null;
        }

        private void ClearSubscriptionRemarks()
        {
            textBoxSubscriptionRemarks.Text = null;
        }

        private void ClearSubscriptionPath()
        {
            textBoxSubscriptionLink.Text = null;
        }

        private void LoadGroupsList()
        {
            Dictionary<Subscription, string> groups;
            InitializeGroups();
            SelectCurrentGroup();

            void InitializeGroups()
            {
                groups = new Dictionary<Subscription, string>();
                foreach(Subscription group in getAllGroups.Invoke())
                    groups.Add(group, group.Directory.Name);
                
                comboBoxSubscription.ItemsSource = groups;
            }

            void SelectCurrentGroup()
            {
                if (!IsAnyGroupExists())
                    return;

                KeyValuePair<Subscription, string> currentGroup = groups.FirstOrDefault(
                    group => group.Key.Directory.FullName == FileUtility.GetDirectory(groupPath)
                );

                if (!IsCurrentGroupExists())
                    currentGroup = groups.Last();
                
                comboBoxSubscription.SelectedValue = currentGroup.Key;

                bool IsCurrentGroupExists() => currentGroup.Key != null && currentGroup.Value != null;
            }

            bool IsAnyGroupExists() => groups.Count > 0;
        }

        private void LoadConfigsList(GroupType group)
        {
            List<Components.Config> configComponents;
            List<Config> configs;
            List<Subscription> groups;
            StackPanel parent;
            TextBlock textNoServer;

            if (group == GroupType.GENERAL)
            {
                generalConfigComponents = new List<Components.Config>();
                configs = getAllGeneralConfigs.Invoke();
                groups = new List<Subscription>();
                parent = listConfigs;
                textNoServer = textNoConfig;
                configComponents = generalConfigComponents;
            }
            else
            {
                subscriptionConfigComponents = new List<Components.Config>();
                configs = getAllSubscriptionConfigs.Invoke(groupPath);
                groups = getAllGroups.Invoke();
                parent = listSubscriptions;
                textNoServer = textNoSubscription;
                configComponents = subscriptionConfigComponents;
                HandleShowingSubscriptionControlPanel();
                
                void HandleShowingSubscriptionControlPanel()
                {
                    if (groups.Count > 0)
                        panelSubscriptionControl.Visibility = Visibility.Visible;
                    else
                        panelSubscriptionControl.Visibility = Visibility.Collapsed;
                }
            }
            
            ClearConfigsList(parent);
            HandleShowingNoServerExistsHint(configs, groups, textNoServer);

            foreach (Config config in configs)
            {
                Components.Config configComponent = CreateConfigComponent(config);
                AddConfigToList(configComponent, configComponents, parent);
            }

            if (IsAnyConfigExists(configs))
                AddConfigHintAtTheEndOfList(parent);

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

            void ClearConfigsList(StackPanel list)
            {
                list.Children.Clear();
            }

            void AddConfigHintAtTheEndOfList(StackPanel list)
            {
                list.Children.Add(new Components.ConfigHint());
            }

            bool IsAnyConfigExists(List<Config> configs)
            {
                return configs != null && configs.Count > 0;
            }

            void HandleShowingNoServerExistsHint(
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
        }

        private string GetLastConfigPath(GroupType group)
        {
            Config lastConfig;
            if (group == GroupType.GENERAL)
            {
                lastConfig = getAllGeneralConfigs.Invoke().LastOrDefault();

                if (!IsConfigExists())
                    lastConfig = getAllSubscriptionConfigs.Invoke(groupPath).LastOrDefault();
            }
            else
            {
                lastConfig = getAllSubscriptionConfigs.Invoke(groupPath).LastOrDefault();

                if (!IsConfigExists())
                    lastConfig = getAllGeneralConfigs.Invoke().LastOrDefault();
            }
                
            if(!IsConfigExists())
                return null;

            return lastConfig.Path;

            bool IsConfigExists() => lastConfig != null;
        } 

        private void SelectConfig(string path)
        {
            DeselectAllConfigs();
            SelectConfig();

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

        private void SetActiveLoadingPanel(bool isActive) => SetActivePanel(panelLoading, isActive);

        private void SetActiveConfigPanel(bool isActive) => SetActivePanel(panelConfig, isActive);

        private void SetActiveSubscriptionPanel(bool isActive) => SetActivePanel(panelSubscription, isActive);

        private void SetActivePanel(Panel panel, bool isActive)
        {
            panel.Visibility = isActive ? Visibility.Visible : Visibility.Hidden;
        }
        
        private void HideAllPanels()
        {
            SetActiveConfigPanel(false);
            SetActiveSubscriptionPanel(false);
        }

        private void SetEnableConfigTabButton(bool isEnabled) => SetEnableButton(buttonConfigTab, isEnabled);

        private void SetEnableSubscriptionTabButton(bool isEnabled) => SetEnableButton(buttonSubscriptionTab, isEnabled);

        private void SetEnableButton(Button button, bool isEnabled)
        {
            button.IsEnabled = isEnabled;
        }

        private void EnableAllTabs()
        {
            SetEnableConfigTabButton(true);
            SetEnableSubscriptionTabButton(true);
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
