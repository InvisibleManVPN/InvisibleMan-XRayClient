using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using Microsoft.Win32;

namespace InvisibleManXRay
{
    using Models;
    using Services;
    using Services.Analytics.ServerWindow;

    public partial class ServerWindow : Window
    {
        private enum ImportingType { FILE, LINK }

        private string configPath = null;
        private ImportingType importingType;
        private List<Components.Config> generalConfigComponents;

        private Func<List<Config>> getAllGeneralConfigs;
        private Func<string, Status> convertLinkToConfig;
        private Func<string, Status> loadConfig;
        private Action<string> onCopyConfig;
        private Action<string, string> onCreateConfig;
        private Action<GroupType, string> onDeleteConfig;
        private Action<string> onUpdateConfig;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public void OpenImportConfigWithLinkSection(string link)
        {
            pendingToRenderActions = () => {
                OnConfigTabClick(null, null);
                OnAddConfigButtonClick(null, null);
                OnLinkRadioButtonClick(null, null);
                radioButtonLink.IsChecked = true;
                textBoxConfigLink.Text = link;
            };
        }

        private void InitializeGeneralConfigComponents()
        {
            generalConfigComponents = new List<Components.Config>();
        }

        private void OnConfigTabClick(object sender, RoutedEventArgs e)
        {
            EnableAllTabs();
            HideAllPanels();

            SetEnableConfigTabButton(false);
            SetActiveConfigPanel(true);
        }

        private void OnAddConfigButtonClick(object sender, RoutedEventArgs e)
        {
            ShowAddConfigsServerPanel();
            AnalyticsService.SendEvent(new AddConfigButtonClickedEvent());
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
                        LocalizationService.GetTerm(Values.Localization.NO_CONFIG_FILE_SELECTED), 
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
                        LocalizationService.GetTerm(Values.Localization.NO_CONFIG_LINK_ENTERED), 
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

        private void ShowAddConfigsServerPanel()
        {
            panelServers.Visibility = Visibility.Hidden;
            panelAddConfigs.Visibility = Visibility.Visible;
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

        private void ClearConfigPath()
        {
            configPath = null;
            textBlockFileName.Text = LocalizationService.GetTerm(Values.Localization.NO_FILE_CHOOSEN);
        }

        private void ClearConfigLink()
        {
            textBoxConfigLink.Text = null;
        }

        private void LoadGeneralConfigsList()
        {
            List<Config> configs = getAllGeneralConfigs.Invoke();
            generalConfigComponents = new List<Components.Config>();

            ClearConfigsList(listConfigs);

            HandleShowingNoServerExistsHint(
                configs: configs,
                groups: new List<Subscription>(),
                textNoServer: textNoConfig
            );

            AddAllConfigsToList(
                configs: configs, 
                configComponents: generalConfigComponents, 
                parent: listConfigs
            );

            if (IsAnyConfigExists(configs))
                AddConfigHintAtTheEndOfList(listConfigs);
        }

        private string GetLastGeneralConfigPath()
        {
            Config lastConfig = getAllGeneralConfigs.Invoke().LastOrDefault();

            if (!IsConfigExists())
                lastConfig = getAllSubscriptionConfigs.Invoke(groupPath).LastOrDefault();
            
            if(!IsConfigExists())
                return null;

            return lastConfig.Path;

            bool IsConfigExists() => lastConfig != null;
        }

        private void SetActiveConfigPanel(bool isActive) => SetActivePanel(panelConfig, isActive);

        private void SetEnableConfigTabButton(bool isEnabled) => SetEnableButton(buttonConfigTab, isEnabled);
    }
}