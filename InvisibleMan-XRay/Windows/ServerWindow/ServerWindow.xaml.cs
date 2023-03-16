using System;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Win32;

namespace InvisibleManXRay
{
    using Models;
    using Values;

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
        private Func<string, bool> testConnection;
        private Action<string> onCopyConfig;
        private Action<string, string> onCreateConfig;
        private Action onDeleteConfig;
        private Action<int> onUpdateConfig;

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
            Func<string, bool> testConnection,
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
            this.onCopyConfig = onCopyConfig;
            this.onCreateConfig = onCreateConfig;
            this.onDeleteConfig = onDeleteConfig;
            this.onUpdateConfig = onUpdateConfig;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            ShowServersPanel();
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddServerPanel();
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
                HandleImportingConfigFromFile();
            else
                HandleImportingConfigFromLink();

            bool IsFileImporting() => importingType == ImportingType.FILE;

            void HandleImportingConfigFromFile()
            {
                if (!IsFileSelected())
                {
                    MessageBox.Show(
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
                            configStatus.Content.ToString(), 
                            Caption.WARNING, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning
                        );
                    }

                    void HandleErrorMessage()
                    {
                        MessageBox.Show(
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
                    testConnection: (configPath) => {
                        Status configStatus = loadConfig.Invoke(configPath);
                        if (configStatus.Code == Code.ERROR)
                            return false;
                            
                        return testConnection.Invoke(configStatus.Content.ToString());
                    }
                );

                return configComponent;
            }

            void ClearConfigsList() => listConfigs.Children.Clear();

            void AddConfigToList(Components.Config configComponent) 
            {
                configComponents.Add(configComponent);
                listConfigs.Children.Add(configComponent);
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
                if (index == configComponents.Count)
                    return;
                
                configComponents[index].SetSelection(true);
            } 
        }
    }
}
