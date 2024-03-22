using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace InvisibleManXRay.Windows
{
    public partial class MainWindow : Window
    {
        private Action onManageServersClick;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Setup(Action onManageServersClick)
        {
            this.onManageServersClick = onManageServersClick;
        }

        private void OnManageServersClick(object sender, PointerPressedEventArgs e) => onManageServersClick.Invoke();
    }
}