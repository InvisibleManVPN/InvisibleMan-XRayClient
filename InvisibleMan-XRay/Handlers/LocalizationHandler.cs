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
            TryApplyCurrentLanguage();
        }

        public ResourceDictionary GetLocalizationResource() => localizationResource;

        public void TryApplyCurrentLanguage()
        {
            try
            {
                ApplyLanguage(getCurrentLanguage.Invoke());
            }
            catch
            {
                ApplyLanguage(Localization.DEFAULT_LANGUAGE);
            }
        }

        private void ApplyLanguage(string language)
        {
            Uri uri = new Uri($"{Directory.LOCALIZATION}/{language}.xaml", UriKind.Relative);
            localizationResource = new ResourceDictionary() { Source = uri };
            
            App.Current.Resources.MergedDictionaries.Add(localizationResource);
        }
    }
}