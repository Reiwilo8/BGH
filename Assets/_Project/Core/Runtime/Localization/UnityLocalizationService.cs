using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Project.Core.Localization
{
    public sealed class UnityLocalizationService : ILocalizationService
    {
        private const string CoreTable = "Core";
        private const string FallbackCode = "en";

        public string CurrentLanguageCode => LocalizationSettings.SelectedLocale?.Identifier.Code ?? FallbackCode;

        public event Action<string> LanguageChanged;

        public string Get(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(CoreTable, key);
        }

        public string Get(string key, params object[] args)
        {
            var raw = Get(key);
            if (args == null || args.Length == 0) return raw;

            try { return string.Format(raw, args); }
            catch (FormatException)
            {
                Debug.LogWarning($"[Localization] Bad format for '{key}': '{raw}'");
                return raw;
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                languageCode = FallbackCode;

            var locale = FindLocale(languageCode) ?? FindLocale(FallbackCode);
            if (locale == null)
            {
                Debug.LogError("[Localization] No locales available. Add at least English (en).");
                return;
            }

            if (LocalizationSettings.SelectedLocale == locale) return;

            LocalizationSettings.SelectedLocale = locale;
            LanguageChanged?.Invoke(locale.Identifier.Code);
        }

        private static Locale FindLocale(string code)
        {
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null) return null;

            return locales.FirstOrDefault(l =>
                string.Equals(l.Identifier.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }
}