using System;
using Avalonia.Interactivity;

namespace InvisibleManXRay.Foundation.Window
{
    public partial class MessageBox : Avalonia.Controls.Window
    {
        public Action<MessageBoxResult> OnReult { get; private set; }

        public MessageBox()
        {
            InitializeComponent();
        }

        public void Setup(
            string title,
            string message,
            Action<MessageBoxResult> onResult
        )
        {
            this.Title = title;
            this.textMessage.Text = message;
            this.OnReult = onResult;
        }

        private void OnConfirmButtonClick(object sender, RoutedEventArgs args)
        {
            OnReult.Invoke(MessageBoxResult.OK);
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            OnReult.Invoke(MessageBoxResult.None);
            base.OnClosed(e);
        }
    }
}