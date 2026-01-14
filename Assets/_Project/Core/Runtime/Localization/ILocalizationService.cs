using System;

namespace Project.Core.Localization
{
    public interface ILocalizationService
    {
        string CurrentLanguageCode { get; }
        event Action<string> LanguageChanged;

        string Get(string key);
        string Get(string key, params object[] args);

        void SetLanguage(string languageCode);
    }
}