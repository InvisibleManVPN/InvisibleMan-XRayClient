using System;
using Avalonia.Markup.Xaml.Styling;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class ThemeHandler : IHandler
    {
        private Func<string> getCurrentTheme;

        public void Setup(Func<string> getCurrentTheme)
        {
            this.getCurrentTheme = getCurrentTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Uri uri = new Uri($"{Directory.THEMES}/{getCurrentTheme.Invoke()}.axaml");
            ResourceInclude themeResource = new ResourceInclude(uri) { Source = uri };
            
            App.Current.Resources.MergedDictionaries.Add(themeResource);
        }
    }
}