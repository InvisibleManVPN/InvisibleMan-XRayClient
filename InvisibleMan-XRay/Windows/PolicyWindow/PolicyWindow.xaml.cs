using System;
using System.Windows;

namespace InvisibleManXRay
{
    using Services;
    using Services.Analytics.General;
    using Services.Analytics.PolicyWindow;

    public partial class PolicyWindow : Window
    {
        private Action onEmailClick;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public PolicyWindow()
        {
            InitializeComponent();
        }

        public void Setup(Action onEmailClick, Action onGenerateClientId)
        {
            this.onEmailClick = onEmailClick;
            onGenerateClientId.Invoke();
            AnalyticsService.SendEvent(new NewUserEvent());
        }

        private void OnEmailClick(object sender, RoutedEventArgs e)
        {
            onEmailClick.Invoke();
            AnalyticsService.SendEvent(new EmailClickedEvent());
        }
    }
}