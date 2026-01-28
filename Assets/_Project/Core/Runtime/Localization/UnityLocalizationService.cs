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
            return SafeFormat(raw, table: CoreTable, key: key, args);
        }

        public string GetFromTable(string table, string key)
        {
            if (string.IsNullOrWhiteSpace(table)) table = CoreTable;
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        }

        public string GetFromTable(string table, string key, params object[] args)
        {
            var raw = GetFromTable(table, key);
            return SafeFormat(raw, table, key, args);
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

        private static string SafeFormat(string raw, string table, string key, object[] args)
        {
            if (args == null || args.Length == 0) return raw;

            try { return string.Format(raw, args); }
            catch (FormatException)
            {
                Debug.LogWarning($"[Localization] Bad format for '{table}:{key}': '{raw}'");
                return raw;
            }
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