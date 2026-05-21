using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace InvisibleGorillaXRay.Mac.Views
{
    using InvisibleGorillaXRay.Services;
    using InvisibleGorillaXRay.Services.Analytics.AboutWindow;

    public partial class AboutWindow : Window
    {
        private Func<string> getApplicationVersion;
        private Func<string> getXRayCoreVersion;
        private Action onEmailClick;
        private Action onWebsiteClick;
        private Action onBugReportingClick;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

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

            textApplicationVersion.Text = getApplicationVersion.Invoke();
            textXRayCoreVersion.Text = getXRayCoreVersion.Invoke();
        }

        private void OnWebsiteClick(object sender, PointerPressedEventArgs e)
        {
            onWebsiteClick.Invoke();
            AnalyticsService.SendEvent(new WebsiteClickedEvent());
        }

        private void OnEmailClick(object sender, PointerPressedEventArgs e)
        {
            onEmailClick.Invoke();
            AnalyticsService.SendEvent(new EmailClickedEvent());
        }
    }
}
