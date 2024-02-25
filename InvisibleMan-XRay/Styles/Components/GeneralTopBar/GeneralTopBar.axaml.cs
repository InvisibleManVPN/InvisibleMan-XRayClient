using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;

namespace InvisibleManXRay.Styles.Components
{
    public partial class GeneralTopBar : UserControl
    {
        public static readonly StyledProperty<int> WidthProperty = AvaloniaProperty.Register<Loading, int>(nameof(Width), defaultValue: 35);
        public static readonly StyledProperty<int> HeightProperty = AvaloniaProperty.Register<Loading, int>(nameof(Height), defaultValue: 35);
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Loading, string>(nameof(Title), defaultValue: "Title");
        public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<Loading, string>(nameof(Description), defaultValue: "Description");
        public static readonly StyledProperty<Geometry> DataProperty = AvaloniaProperty.Register<GeneralTopBar, Geometry>(nameof(Data), defaultValue: null);

        public GeneralTopBar()
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

        public string Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Description
        {
            get { return GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public Geometry Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}