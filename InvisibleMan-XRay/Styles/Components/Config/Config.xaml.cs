using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    using Values;

    public partial class Config : UserControl
    {
        private Models.Config config;
        
        private Action onSelect;
        private Action onDelete;
        private Func<Window> getServerWindow;
        private Func<string, bool> testConnection;

        private BackgroundWorker checkConnectionWorker;

        public Config()
        {
            InitializeComponent();
            InitializeCheckConnectionWorker();

            void InitializeCheckConnectionWorker()
            {
                checkConnectionWorker = new BackgroundWorker();
                checkConnectionWorker.DoWork += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowLoadingProgress();
                    }));

                    bool isConnectionAvailable = testConnection.Invoke(config.Path);

                    Dispatcher.BeginInvoke(new Action(delegate {
                        Models.Availability availability = isConnectionAvailable ? Models.Availability.AVAILABLE : Models.Availability.TIMEOUT;
                        HandleConfigStatus(availability);
                        ShowCheckButton();
                    }));
                };
            }
        }

        public void Setup(
            Models.Config config, 
            Action onSelect, 
            Action onDelete, 
            Func<Window> getServerWindow,
            Func<string, bool> testConnection)
        {
            this.config = config;
            this.onSelect = onSelect;
            this.onDelete = onDelete;
            this.getServerWindow = getServerWindow;
            this.testConnection = testConnection;

            UpdateUI();
        }

        public void SetSelection(bool isSelect)
        {
            Visibility visibility = isSelect ? Visibility.Visible : Visibility.Hidden;
            gridSelect.Visibility = visibility;
        }

        private void UpdateUI()
        {
            textConfigName.Content = config.Name;
            textUpdateTime.Content = config.UpdateTime;
            
            HandleConfigStatus(config.Availability);
        }

        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
        {
            onSelect.Invoke();
        }

        private void OnEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(config.Path))
            {
                MessageBox.Show(
                    getServerWindow.Invoke(),
                    Message.FILE_DOESNT_EXISTS,
                    Caption.ERROR,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            OpenFileInTextEditor();

            void OpenFileInTextEditor()
            {
                Process fileOpenProcess = new Process();
                fileOpenProcess.StartInfo.FileName = "notepad";
                fileOpenProcess.StartInfo.Arguments = $"\"{config.Path}\"";
                fileOpenProcess.Start();
            }
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                getServerWindow.Invoke(),
                string.Format(Message.DELETE_CONFIRMATION, config.Name),
                Caption.INFO,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
                DeleteFile();
            
            void DeleteFile()
            {
                try
                {
                    File.Delete(config.Path);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(getServerWindow.Invoke(), ex.Message);
                }
                finally
                {
                    onDelete.Invoke();
                }
            }
        }

        private void OnCheckButtonClick(object sender, RoutedEventArgs e)
        {
            checkConnectionWorker.RunWorkerAsync();
        }

        private void HandleConfigStatus(Models.Availability availability)
        {
            config.SetAvailability(availability);

            switch(availability)
            {
                case Models.Availability.NOT_CHECKED:
                    ShowNotCheckedStatus();
                    break;
                case Models.Availability.AVAILABLE:
                    ShowAvailableStatus();
                    break;
                case Models.Availability.TIMEOUT:
                    ShowTimeoutStatus();
                    break;
                default:
                    break;
            }
        }

        private void ShowNotCheckedStatus()
        {
            statusNotChecked.Visibility = Visibility.Visible;
            statusAvailable.Visibility = Visibility.Hidden;
            statusTimeout.Visibility = Visibility.Hidden;
        }

        private void ShowAvailableStatus()
        {
            statusAvailable.Visibility = Visibility.Visible;
            statusNotChecked.Visibility = Visibility.Hidden;
            statusTimeout.Visibility = Visibility.Hidden;
        }

        private void ShowTimeoutStatus()
        {
            statusTimeout.Visibility = Visibility.Visible;
            statusNotChecked.Visibility = Visibility.Hidden;
            statusAvailable.Visibility = Visibility.Hidden;
        }

        private void ShowLoadingProgress()
        {
            progressLoading.Visibility = Visibility.Visible;
            buttonCheck.Visibility = Visibility.Collapsed;
        }

        private void ShowCheckButton()
        {
            buttonCheck.Visibility = Visibility.Visible;
            progressLoading.Visibility = Visibility.Collapsed;
        }
    }
}
