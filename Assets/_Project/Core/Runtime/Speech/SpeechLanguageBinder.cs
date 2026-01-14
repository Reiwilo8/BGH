using System;
using Project.Core.Localization;

namespace Project.Core.Speech
{
    public sealed class SpeechLanguageBinder : IDisposable
    {
        private readonly ILocalizationService _loc;
        private readonly ISpeechService _speech;

        public SpeechLanguageBinder(ILocalizationService loc, ISpeechService speech)
        {
            _loc = loc;
            _speech = speech;

            _loc.LanguageChanged += OnLanguageChanged;

            _speech.SetLanguage(_loc.CurrentLanguageCode);
        }

        private void OnLanguageChanged(string code)
        {
            _speech.SetLanguage(code);
        }

        public void Dispose()
        {
            _loc.LanguageChanged -= OnLanguageChanged;
        }
    }
}