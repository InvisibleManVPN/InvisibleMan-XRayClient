using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace InvisibleManXRay.Components
{
    public partial class Broadcast : UserControl
    {
        private Models.Broadcast broadcast;
        private Storyboard appearStoryboard;
        private Storyboard disappearStoryboard;

        private Action<string> onUrlClick;

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
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            disappearStoryboard.Begin();
        }

        private void OnBroadcastBarClick(object sender,RoutedEventArgs e)
        {
            if (IsClickableBroadcast())
                onUrlClick.Invoke(broadcast.Url);
        }

        private bool IsClickableBroadcast() => !string.IsNullOrEmpty(broadcast.Url);
    }
}
