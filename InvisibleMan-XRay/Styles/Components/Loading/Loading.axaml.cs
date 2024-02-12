using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;

namespace InvisibleManXRay.Styles.Components
{
    public partial class Loading : UserControl
    {
        public static readonly StyledProperty<int> WidthProperty = AvaloniaProperty.Register<Loading, int>(nameof(Width), defaultValue: 120);
        public static readonly StyledProperty<int> HeightProperty = AvaloniaProperty.Register<Loading, int>(nameof(Height), defaultValue: 120);
        public static readonly StyledProperty<IBrush> StrokeProperty = AvaloniaProperty.Register<Loading, IBrush>(nameof(Stroke), defaultValue: new SolidColorBrush(Color.Parse("#fff")));

        public Loading()
        {
            InitializeComponent();
        }

        public int Width
        {
            get { return GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public int Height
        {
            get { return GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        public IBrush Stroke
        {
            get { return GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
    }
}