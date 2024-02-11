using System;
using Avalonia.Controls;

namespace InvisibleManXRay.Services
{
    using Factories;
    using Windows;

    public class WindowsService : IService
    {
        private WindowsFactory windowsFactory;

        public void Setup(WindowsFactory windowsFactory)
        {
            this.windowsFactory = windowsFactory;
        }

        public T OpenWindow<T>() where T : Window
        {
            string type = typeof(T).Name;

            switch(type)
            {
                case nameof(MainWindow):
                    return windowsFactory.CreateMainWindow() as T;
                default:
                    throw new Exception($"The window of type '{type}' does not found.");
            }
        }
    }
}