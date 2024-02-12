using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;

namespace InvisibleManXRay.Styles.Components
{
    public partial class Glow : UserControl
    {
        public static readonly StyledProperty<int> WidthProperty = AvaloniaProperty.Register<Glow, int>(nameof(Width), defaultValue: 120);
        public static readonly StyledProperty<int> HeightProperty = AvaloniaProperty.Register<Glow, int>(nameof(Height), defaultValue: 120);
        public static readonly StyledProperty<IBrush> FillProperty = AvaloniaProperty.Register<Glow, IBrush>(nameof(Fill), defaultValue: new SolidColorBrush(Color.Parse("#fff")));

        public Glow()
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

        public IBrush Fill
        {
            get { return GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
    }
}