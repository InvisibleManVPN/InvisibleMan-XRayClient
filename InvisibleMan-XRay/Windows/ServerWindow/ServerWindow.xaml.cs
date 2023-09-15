using System;
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

    public partial class ServerWindow : Window
    {
        private enum ImportingType { FILE, LINK };

        private string configPath = null;
        private ImportingType importingType;
        private List<Components.Config> configComponents;

        private Func<int> getCurrentConfigIndex;
        private Func<List<Config>> getAllConfigs;
        private Func<string, Status> convertConfigLinkToV2Ray;
        private Func<string, Status> loadConfig;
        private Func<string, int> testConnection;
        private Func<string> getLogPath;
        private Action<string> onCopyConfig;
        private Action<string, string> onCreateConfig;
        private Action onDeleteConfig;
        private Action<int> onUpdateConfig;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public ServerWindow()
        {
            InitializeComponent();
            InitializeImportingGroups();

            void InitializeImportingGroups()
            {
                SetActiveFileImportingGroup(true);
                SetActiveLinkImportingGroup(false);
                SetImportingType(ImportingType.FILE);
            }
        }

        public void Setup(
            Func<int> getCurrentConfigIndex,
            Func<List<Config>> getAllConfigs, 
            Func<string, Status> convertConfigLinkToV2Ray,
            Func<string, Status> loadConfig, 
            Func<string, int> testConnection,
            Func<string> getLogPath,
            Action<string> onCopyConfig,
            Action<string, string> onCreateConfig,
            Action onDeleteConfig,
            Action<int> onUpdateConfig)
        {
            this.getCurrentConfigIndex = getCurrentConfigIndex;
            this.getAllConfigs = getAllConfigs;
            this.convertConfigLinkToV2Ray = convertConfigLinkToV2Ray;
            this.loadConfig = loadConfig;
            this.testConnection = testConnection;
            this.getLogPath = getLogPath;
            this.onCopyConfig = onCopyConfig;
            this.onCreateConfig = onCreateConfig;
            this.onDeleteConfig = onDeleteConfig;
            this.onUpdateConfig = onUpdateConfig;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            ShowServersPanel();
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

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddServerPanel();
            AnalyticsService.SendEvent(new AddConfigButtonClickedEvent());
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

        private void OnImportButtonClick(object sender, RoutedEventArgs e)
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
                        Values.Message.NO_FILES_SELECTED, 
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
                        Values.Message.NO_LINK_ENTERED, 
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

            void SetActiveLoadingPanel(bool isActive)
            {
                Visibility visibility = isActive ? Visibility.Visible : Visibility.Hidden;
                panelLoading.Visibility = visibility;
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
                    configStatus = convertConfigLinkToV2Ray.Invoke(textBoxConfigLink.Text);
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
                
                onUpdateConfig.Invoke(GetLastConfigIndex());
                SetActiveLoadingPanel(false);
                ClearConfigPath();
                ClearConfigLink();
                ShowServersPanel();

                void HandleError()
                {
                    switch (configStatus.SubCode)
                    {
                        case SubCode.NO_CONFIG:
                        case SubCode.UNSUPPORTED_LINK:
                            HandleWarningMessage();
                            break;
                        case SubCode.INVALID_CONFIG:
                            HandleErrorMessage();
                            break;
                        default:
                            return;
                    }

                    void HandleWarningMessage()
                    {
                        MessageBoxResult result = MessageBox.Show(
                            this,
                            configStatus.Content.ToString(), 
                            Caption.WARNING, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning
                        );
                    }

                    void HandleErrorMessage()
                    {
                        MessageBox.Show(
                            this,
                            configStatus.Content.ToString(), 
                            Caption.ERROR, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error
                        );
                    }
                }
            }
        }

        private void ShowAddServerPanel()
        {
            panelServers.Visibility = Visibility.Hidden;
            panelAdd.Visibility = Visibility.Visible;
        }

        private void ShowServersPanel()
        {
            panelAdd.Visibility = Visibility.Hidden;
            panelServers.Visibility = Visibility.Visible;
            LoadConfigsList();
            SelectConfig(getCurrentConfigIndex.Invoke());
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

        private void LoadConfigsList()
        {
            configComponents = new List<Components.Config>();
            List<Config> configs = getAllConfigs.Invoke();

            ClearConfigsList();
            HandleShowingNoServerExistsHint();

            foreach (Config config in configs)
            {
                Components.Config configComponent = CreateConfigComponent(config);
                AddConfigToList(configComponent);
            }

            if (IsAnyConfigExists())
                AddConfigHintAtTheEndOfList();

            Components.Config CreateConfigComponent(Config config)
            {
                Components.Config configComponent = new Components.Config();
                configComponent.Setup(
                    config: config, 
                    onSelect: () => {
                        int selectedConfigIndex = getAllConfigs.Invoke().FindIndex(
                            item => item == config
                        );
                        onUpdateConfig.Invoke(selectedConfigIndex);
                        SelectConfig(selectedConfigIndex);
                    },
                    onDelete: () => {
                        onDeleteConfig.Invoke();
                        LoadConfigsList();
                        HandleCurrentConfigIndex();
                        SelectConfig(getCurrentConfigIndex.Invoke());

                        void HandleCurrentConfigIndex()
                        {
                            int currentConfigIndex = getCurrentConfigIndex.Invoke();
                            int deletedConfigIndex = getAllConfigs.Invoke().FindIndex(
                                item => item == config
                            );

                            if (deletedConfigIndex == -1)
                                onUpdateConfig.Invoke(GetLastConfigIndex());
                            else if (deletedConfigIndex < currentConfigIndex)
                                onUpdateConfig.Invoke(currentConfigIndex - 1);
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

            void ClearConfigsList() => listConfigs.Children.Clear();

            void AddConfigToList(Components.Config configComponent) 
            {
                configComponents.Add(configComponent);
                listConfigs.Children.Add(configComponent);
            }

            bool IsAnyConfigExists()
            {
                return configs != null && configs.Count > 0;
            }

            void AddConfigHintAtTheEndOfList()
            {
                listConfigs.Children.Add(new Components.ConfigHint());
            }

            void HandleShowingNoServerExistsHint()
            {
                if (configs.Count > 0)
                    textNoServer.Visibility = Visibility.Collapsed;
                else
                    textNoServer.Visibility = Visibility.Visible;
            }
        }

        private int GetLastConfigIndex()
        {
            int configsCount = getAllConfigs.Invoke().Count;

            if (configsCount == 0)
                return 0;
            return configsCount - 1;
        } 

        private void SelectConfig(int index)
        {
            DeselectAllConfigs();
            SelectConfig();

            void DeselectAllConfigs() => configComponents.ForEach(
                configComponent => configComponent.SetSelection(false)
            );
            
            void SelectConfig()
            {
                if (IsIndexOutOfRange())
                    return;
                
                configComponents[index].SetSelection(true);

                bool IsIndexOutOfRange() => index >= configComponents.Count;
            } 
        }

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
    }
}
