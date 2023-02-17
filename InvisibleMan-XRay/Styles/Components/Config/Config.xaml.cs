using System.Windows.Controls;

namespace InvisibleManXRay.Components
{
    public partial class Config : UserControl
    {
        public Config()
        {
            InitializeComponent();
        }

        public void Setup(Models.Config config)
        {
            textConfigName.Content = config.Name;
            textUpdateTime.Content = config.UpdateTime;
        }
    }
}
