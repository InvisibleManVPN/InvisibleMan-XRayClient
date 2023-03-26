using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace InvisibleManXRay.Components
{
    public partial class Broadcast : UserControl
    {
        private Storyboard disappearStoryboard;

        public Broadcast()
        {
            InitializeComponent();
            InitializeDisappearStoryboard();

            void InitializeDisappearStoryboard() 
            {
                disappearStoryboard = TryFindResource("Disappear") as Storyboard;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            disappearStoryboard.Begin();
        }
    }
}
