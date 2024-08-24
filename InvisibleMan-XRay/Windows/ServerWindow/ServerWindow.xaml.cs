using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace InvisibleManXRay
{
    using Models;
    using Values;
    using Services;

    public partial class ServerWindow : Window
    {
        private Action pendingToRenderActions = delegate { };

        private Func<string> getCurrentConfigPath;
        private Func<bool> isCurrentPathEqualRootConfigPath;
        private Func<string, int> testConnection;
        private Func<string> getLogPath;

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
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ShowServersPanel();
        }

        private void ShowServersPanel()
        {
            panelAddConfigs.Visibility = Visibility.Hidden;
            panelAddSubscriptions.Visibility = Visibility.Hidden;
            panelServers.Visibility = Visibility.Visible;
            SelectConfig(getCurrentConfigPath.Invoke());
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
