using System;
using System.Windows;

namespace InvisibleManXRay.Services
{
    public class LocalizationService : Service
    {
        private Func<ResourceDictionary> getLocalizationResource;

        private ResourceDictionary LocalizationResource => getLocalizationResource.Invoke();

        public void Setup(Func<ResourceDictionary> getLocalizationResource)
        {
            this.getLocalizationResource = getLocalizationResource;
        }

        public string GetTerm(string key)
        {
            object value = LocalizationResource[key];
            return value as string;
        }
    }
}