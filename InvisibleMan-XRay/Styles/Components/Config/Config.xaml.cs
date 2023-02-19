using System;
using System.IO;
using System.Diagnostics;
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

        public Config()
        {
            InitializeComponent();
        }

        public void Setup(Models.Config config, Action onSelect, Action onDelete)
        {
            this.config = config;
            this.onSelect = onSelect;
            this.onDelete = onDelete;

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
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    onDelete.Invoke();
                }
            }
        }
    }
}
