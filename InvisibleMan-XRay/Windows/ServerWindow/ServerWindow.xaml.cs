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
        private string configPath = null;
        private Func<List<Config>> getAllConfigs;
        private Func<string, Status> loadConfig;
        private Action<string> onAddConfig;

        public ServerWindow()
        {
            InitializeComponent();
        }

        public void Setup(
            Func<List<Config>> getAllConfigs, 
            Func<string, Status> loadConfig, 
            Action<string> onAddConfig)
        {
            this.getAllConfigs = getAllConfigs;
            this.loadConfig = loadConfig;
            this.onAddConfig = onAddConfig;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            GoToServersPanel();
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            GoToAddServerPanel();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            GoToServersPanel();
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
            if (!IsFileSelected())
            {
                MessageBox.Show(
                    Values.Message.NO_FILES_SELECTED, 
                    Values.Caption.ERROR, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
                return;
            }

            SetActiveLoadingPanel(true);
            TryAddConfig();

            bool IsFileSelected() => !string.IsNullOrEmpty(configPath);

            void SetActiveLoadingPanel(bool isActive)
            {
                Visibility visibility = isActive ? Visibility.Visible : Visibility.Hidden;
                panelLoading.Visibility = visibility;
            }

            void ClearConfigPath()
            {
                configPath = null;
                textBlockFileName.Text = "No file chosen...";
            }

            void TryAddConfig()
            {
                Status configStatus = loadConfig.Invoke(configPath);

                if (configStatus.Code == Code.ERROR)
                {
                    HandleError();
                    SetActiveLoadingPanel(false);
                    return;
                }

                onAddConfig.Invoke(configPath);
                SetActiveLoadingPanel(false);
                ClearConfigPath();
                GoToServersPanel();

                void HandleError()
                {
                    switch (configStatus.SubCode)
                    {
                        case SubCode.NO_CONFIG:
                            HandleNoConfigError();
                            break;
                        case SubCode.INVALID_CONFIG:
                            HandleInvalidConfigError();
                            break;
                        default:
                            return;
                    }

                    void HandleNoConfigError()
                    {
                        MessageBoxResult result = MessageBox.Show(
                            configStatus.Content, 
                            Caption.WARNING, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning
                        );
                    }

                    void HandleInvalidConfigError()
                    {
                        MessageBox.Show(
                            configStatus.Content, 
                            Caption.ERROR, 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error
                        );
                    }
                }
            }
        }

        private void GoToAddServerPanel()
        {
            panelServers.Visibility = Visibility.Hidden;
            panelAdd.Visibility = Visibility.Visible;
        }

        private void GoToServersPanel()
        {
            panelAdd.Visibility = Visibility.Hidden;
            panelServers.Visibility = Visibility.Visible;
            LoadConfigsList();
        }

        private void LoadConfigsList()
        {
            List<Config> configs = getAllConfigs.Invoke();
            
            if (configs.Count > 0)
                ClearConfigsList();

            foreach (Config config in configs)
            {
                Components.Config configComponent = CreateConfigComponent(config);
                AddConfigToList(configComponent);
            }

            Components.Config CreateConfigComponent(Config config)
            {
                Components.Config configComponent = new Components.Config();
                configComponent.Setup(config);
                return configComponent;
            }

            void AddConfigToList(Components.Config configComponent)
            {
                textNoServer.Visibility = Visibility.Collapsed;
                listConfigs.Children.Add(configComponent);
            }

            void ClearConfigsList() => listConfigs.Children.Clear();
        }
    }
}
