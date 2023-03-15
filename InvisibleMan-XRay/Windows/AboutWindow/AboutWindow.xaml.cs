using System;
using System.Windows;

namespace InvisibleManXRay
{
    public partial class AboutWindow : Window
    {
        private Action onEmailClick;
        private Action onWebsiteClick;
        private Action onBugReportingClick;

        public AboutWindow()
        {
            InitializeComponent();
        }

        public void Setup(
            Action onEmailClick,
            Action onWebsiteClick,
            Action onBugReportingClick)
        {
            this.onEmailClick = onEmailClick;
            this.onWebsiteClick = onWebsiteClick;
            this.onBugReportingClick = onBugReportingClick;
        }

        private void OnWebsiteClick(object sender, RoutedEventArgs e) => onWebsiteClick.Invoke();

        private void OnBugReportingClick(object sender, RoutedEventArgs e) => onBugReportingClick.Invoke();

        private void OnEmailClick(object sender, RoutedEventArgs e) => onEmailClick.Invoke();
    }
}
