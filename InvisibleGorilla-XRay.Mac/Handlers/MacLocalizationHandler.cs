using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InvisibleGorillaXRay.Mac.Handlers
{
    using InvisibleGorillaXRay.Handlers;
    using InvisibleGorillaXRay.Values;

    public class MacLocalizationHandler : Handler
    {
        private Func<string> getCurrentLanguage;
        private Dictionary<string, string> terms = new();
        private ResourceDictionary? currentLangDict;

        public void Setup(Func<string> getCurrentLanguage)
        {
            this.getCurrentLanguage = getCurrentLanguage;
            TryApplyCurrentLanguage();
        }

        public string GetTerm(string key)
        {
            if (terms.TryGetValue(key, out var value))
                return value;

            if (Application.Current != null &&
                Application.Current.TryFindResource(key, out var res) &&
                res is string str)
            {
                terms[key] = str;
                return str;
            }

            return key;
        }

        public void TryApplyCurrentLanguage()
        {
            try { ApplyLanguage(getCurrentLanguage.Invoke()); }
            catch { ApplyLanguage(Localization.DEFAULT_LANGUAGE); }
        }

        private void ApplyLanguage(string language)
        {
            terms.Clear();
            try
            {
                if (currentLangDict != null && Application.Current != null)
                    Application.Current.Resources.MergedDictionaries.Remove(currentLangDict);

                var uri = new Uri($"avares://InvisibleGorilla-XRay.Mac/Assets/Localization/{language}.axaml");
                var dict = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);

                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        if (kv.Key is string k && kv.Value is string v)
                            terms[k] = v;
                    }

                    currentLangDict = dict;
                    if (Application.Current != null)
                        Application.Current.Resources.MergedDictionaries.Add(dict);
                }
            }
            catch { }
        }
    }
}
