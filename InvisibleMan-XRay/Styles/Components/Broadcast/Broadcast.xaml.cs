using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace InvisibleManXRay.Components
{
    using Services;
    using Services.Analytics.Broadcast;

    public partial class Broadcast : UserControl
    {
        private Models.Broadcast broadcast;
        private Storyboard appearStoryboard;
        private Storyboard disappearStoryboard;

        private Action<string> onUrlClick;

        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public Broadcast()
        {
            InitializeComponent();
            InitializeStoryboards();

            void InitializeStoryboards() 
            {
                appearStoryboard = TryFindResource("Appear") as Storyboard;
                disappearStoryboard = TryFindResource("Disappear") as Storyboard;
            }
        }

        public void Setup(Models.Broadcast broadcast, Action<string> onUrlClick)
        {
            this.broadcast = broadcast;
            this.onUrlClick = onUrlClick;
            UpdateUI();

            void UpdateUI()
            {
                textBroadcast.Content = broadcast.Text;
                if (IsClickableBroadcast())
                    textBroadcast.Cursor = System.Windows.Input.Cursors.Hand;
            }
        }

        public void Appear()
        {
            this.Visibility = Visibility.Visible;
            appearStoryboard.Begin();
            AnalyticsService.SendEvent(new ShownEvent());
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            disappearStoryboard.Begin();
            AnalyticsService.SendEvent(new ClosedEvent());
        }

        private void OnBroadcastBarClick(object sender,RoutedEventArgs e)
        {
            if (IsClickableBroadcast())
            {
                onUrlClick.Invoke(broadcast.Url);
                AnalyticsService.SendEvent(new ClickedEvent());
            }
        }

        private bool IsClickableBroadcast() => !string.IsNullOrEmpty(broadcast.Url);
    }
}
