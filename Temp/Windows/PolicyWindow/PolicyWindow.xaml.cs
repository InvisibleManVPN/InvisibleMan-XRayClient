using System;
using System.Windows;

namespace InvisibleManXRay
{
    using Services;
    using Services.Analytics.PolicyWindow;

    public partial class PolicyWindow : Window
    {
        private Action onEmailClick;

        public PolicyWindow()
        {
            InitializeComponent();
        }

        public void Setup(Action onEmailClick)
        {
            this.onEmailClick = onEmailClick;
        }

        private void OnEmailClick(object sender, RoutedEventArgs e)
        {
            onEmailClick.Invoke();
            ServiceLocator.Get<AnalyticsService>().SendEvent(new EmailClickedEvent());
        }
    }
}