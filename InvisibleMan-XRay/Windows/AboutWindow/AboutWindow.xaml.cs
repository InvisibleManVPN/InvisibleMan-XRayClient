using System;
using System.Windows;

namespace InvisibleManXRay
{
    public partial class AboutWindow : Window
    {
        private Func<string> getApplicationVersion;
        private Func<string> getXRayCoreVersion;
        private Action onEmailClick;
        private Action onWebsiteClick;
        private Action onBugReportingClick;

        public AboutWindow()
        {
            InitializeComponent();
        }

        public void Setup(
            Func<string> getApplicationVersion,
            Func<string> getXRayCoreVersion,
            Action onEmailClick,
            Action onWebsiteClick,
            Action onBugReportingClick)
        {
            this.getApplicationVersion = getApplicationVersion;
            this.getXRayCoreVersion = getXRayCoreVersion;
            this.onEmailClick = onEmailClick;
            this.onWebsiteClick = onWebsiteClick;
            this.onBugReportingClick = onBugReportingClick;

            UpdateVersionsUI();

            void UpdateVersionsUI()
            {
                textApplicationVersion.Text = getApplicationVersion.Invoke();
                textXRayCoreVersion.Text = getXRayCoreVersion.Invoke();
            }
        }

        private void OnWebsiteClick(object sender, RoutedEventArgs e) => onWebsiteClick.Invoke();

        private void OnBugReportingClick(object sender, RoutedEventArgs e) => onBugReportingClick.Invoke();

        private void OnEmailClick(object sender, RoutedEventArgs e) => onEmailClick.Invoke();
    }
}
