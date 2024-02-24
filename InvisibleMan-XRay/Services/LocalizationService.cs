using System;
using Avalonia.Markup.Xaml.Styling;

namespace InvisibleManXRay.Services
{
    public class LocalizationService : IService
    {
        private Func<ResourceInclude> getLocalizationResource;

        private ResourceInclude LocalizationResource => getLocalizationResource.Invoke();

        public void Setup(Func<ResourceInclude> getLocalizationResource)
        {
            this.getLocalizationResource = getLocalizationResource;
        }

        public string GetTerm(string key)
        {
            LocalizationResource.TryGetResource(key, null, out object value);
            return value as string;
        }
    }
}