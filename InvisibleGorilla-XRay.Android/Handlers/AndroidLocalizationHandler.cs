using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InvisibleGorillaXRay.Android.Handlers
{
    using InvisibleGorillaXRay.Handlers;
    using InvisibleGorillaXRay.Values;

    public sealed class AndroidLocalizationHandler : Handler
    {
        private Func<string>? getCurrentLanguage;
        private readonly Dictionary<string, string> terms = new();
        private bool isLanguageLoaded;
        private ResourceDictionary? currentLangDict;

        public void Setup(Func<string> getCurrentLanguage)
        {
            this.getCurrentLanguage = getCurrentLanguage;
        }

        public string GetTerm(string key)
        {
            EnsureLanguageLoaded();

            if (terms.TryGetValue(key, out string? value))
                return value;

            if (Application.Current != null &&
                Application.Current.TryFindResource(key, out object? res) &&
                res is string str)
            {
                terms[key] = str;
                return str;
            }

            return key;
        }

        public void TryApplyCurrentLanguage()
        {
            try
            {
                ApplyLanguage(getCurrentLanguage?.Invoke() ?? Localization.DEFAULT_LANGUAGE);
                isLanguageLoaded = true;
            }
            catch
            {
                ApplyLanguage(Localization.DEFAULT_LANGUAGE);
                isLanguageLoaded = true;
            }
        }

        private void EnsureLanguageLoaded()
        {
            if (isLanguageLoaded)
                return;

            TryApplyCurrentLanguage();
        }

        private void ApplyLanguage(string language)
        {
            terms.Clear();

            try
            {
                ResourceDictionary dict = LoadDictionary(language);
                if (currentLangDict != null && Application.Current != null)
                    Application.Current.Resources.MergedDictionaries.Remove(currentLangDict);

                currentLangDict = dict;

                if (Application.Current != null)
                    Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            catch
            {
            }
        }

        private static ResourceDictionary LoadDictionary(string language)
        {
            Exception? lastException = null;

            string[] candidates =
            {
                $"avares://InvisibleGorilla-XRay.Android/Assets/Localization/{language}.axaml",
                $"avares://InvisibleGorillaXRay.Android/Assets/Localization/{language}.axaml",
                $"avares://InvisibleGorilla-XRay.Mac/Assets/Localization/{language}.axaml",
                $"avares://InvisibleGorillaXRay.Mac/Assets/Localization/{language}.axaml"
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    return (ResourceDictionary)AvaloniaXamlLoader.Load(new Uri(candidate));
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw lastException ?? new InvalidOperationException("Localization resource dictionary not found.");
        }
    }
}
