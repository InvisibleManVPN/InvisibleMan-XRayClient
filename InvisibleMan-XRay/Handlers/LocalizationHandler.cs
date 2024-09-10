using System;
using System.Windows;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LocalizationHandler : Handler
    {
        private Func<string> getCurrentLanguage;
        private ResourceDictionary localizationResource;

        public void Setup(Func<string> getCurrentLanguage)
        {
            this.getCurrentLanguage = getCurrentLanguage;
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Uri uri = new Uri($"{Directory.LOCALIZATION}/{getCurrentLanguage.Invoke()}.xaml", UriKind.Relative);
            localizationResource = new ResourceDictionary() { Source = uri };
            
            App.Current.Resources.MergedDictionaries.Add(localizationResource);
        }
    }
}