using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    public partial class Config : UserControl
    {
        private Models.Config config;

        public Config()
        {
            InitializeComponent();
        }

        public void Setup(Models.Config config)
        {
            this.config = config;
            UpdateUI();
        }

        private void UpdateUI()
        {
            textConfigName.Content = config.Name;
            textUpdateTime.Content = config.UpdateTime;
        }

        private void OnEditButtonClick(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(config.Path))
            {
                MessageBox.Show(
                    Values.Message.FILE_DOESNT_EXISTS,
                    Values.Caption.ERROR,
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
    }
}
