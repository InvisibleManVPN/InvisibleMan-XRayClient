using System;

namespace InvisibleManXRay.Foundation
{
    public static class MessageBox
    {
        private static Window.MessageBox Instance;

        public static void Show(
            string message,
            Action<MessageBoxResult> onResult
        )
        {
            Instance = new Window.MessageBox();
            Instance.Setup(
                title: "",
                message: message,
                onResult: onResult
            );
            Instance.Show();
        }

        public static void Show(
            string title,
            string message,
            Action<MessageBoxResult> onResult
        )
        {
            Instance = new Window.MessageBox();
            Instance.Setup(
                title: title,
                message: message, 
                onResult: onResult
            );
            Instance.Show();
        }
    }
}