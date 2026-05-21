using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace InvisibleGorillaXRay.Mac.Views
{
    using InvisibleGorillaXRay.Services;
    using InvisibleGorillaXRay.Services.Analytics.PolicyWindow;

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

        private void OnEmailClick(object sender, PointerPressedEventArgs e)
        {
            onEmailClick?.Invoke();
            ServiceLocator.Get<AnalyticsService>().SendEvent(new EmailClickedEvent());
        }
    }
}
