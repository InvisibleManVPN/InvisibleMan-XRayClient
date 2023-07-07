using System;
using System.Windows;

namespace InvisibleManXRay
{
    using Services;
    using Services.Analytics.General;

    public partial class PolicyWindow : Window
    {
        public PolicyWindow()
        {
            InitializeComponent();
        }

        public void Setup(Action onGenerateClientId)
        {
            onGenerateClientId.Invoke();
            ServiceLocator.Get<AnalyticsService>().SendEvent(new NewUserEvent());
        }
    }
}