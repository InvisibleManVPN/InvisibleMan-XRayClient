using System;
using Avalonia.Markup.Xaml.Styling;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LocalizationHandler : IHandler
    {
        private Func<string> getCurrentLanguage;

        public void Setup(Func<string> getCurrentLanguage)
        {
            this.getCurrentLanguage = getCurrentLanguage;
            ApplyLanguage();
        }
        
        private void ApplyLanguage()
        {
            Uri uri = new Uri($"{Directory.LOCALIZATION}/{getCurrentLanguage.Invoke()}.axaml");
            App.Current.Resources.MergedDictionaries.Add(
                item: new ResourceInclude(uri) { Source = uri }
            );
        }
    }
}