using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    public partial class Loading : UserControl
    {
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
            "Foreground", typeof(Brush), typeof(Loading)
        );

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public Loading()
        {
            InitializeComponent();
        }
    }
}
