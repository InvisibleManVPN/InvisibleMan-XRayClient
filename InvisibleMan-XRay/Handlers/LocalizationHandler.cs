using System;
using Avalonia.Markup.Xaml.Styling;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LocalizationHandler : IHandler
    {
        private Func<string> getCurrentLanguage;
        private ResourceInclude localizationResource;

        public void Setup(Func<string> getCurrentLanguage)
        {
            this.getCurrentLanguage = getCurrentLanguage;
            ApplyLanguage();
        }

        public ResourceInclude GetLocalizationResource() => localizationResource;
        
        private void ApplyLanguage()
        {
            Uri uri = new Uri($"{Directory.LOCALIZATION}/{getCurrentLanguage.Invoke()}.axaml");
            localizationResource = new ResourceInclude(uri) { Source = uri };
            
            App.Current.Resources.MergedDictionaries.Add(localizationResource);
        }
    }
}