using Project.Core.Localization;

namespace Project.Core.Speech
{
    public sealed class SpeechLocalizer : ISpeechLocalizer
    {
        private readonly ILocalizationService _loc;
        private readonly ISpeechService _speech;

        public SpeechLocalizer(ILocalizationService loc, ISpeechService speech)
        {
            _loc = loc;
            _speech = speech;
        }

        public void SpeakKey(string key, SpeechPriority priority = SpeechPriority.Normal)
            => _speech.Speak(_loc.Get(key), priority);

        public void SpeakKey(string key, SpeechPriority priority, params object[] args)
            => _speech.Speak(_loc.Get(key, args), priority);

        public void StopAll() => _speech.StopAll();
    }
}