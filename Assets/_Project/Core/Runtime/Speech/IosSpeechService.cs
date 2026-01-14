#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Project.Core.Speech
{
    public sealed class IosSpeechService : ISpeechService
    {
        [DllImport("__Internal")] private static extern void TTS_Init();
        [DllImport("__Internal")] private static extern void TTS_Speak(string text, string lang);
        [DllImport("__Internal")] private static extern void TTS_Stop();
        [DllImport("__Internal")] private static extern bool TTS_IsSpeaking();

        private string _lang = "en-US";

        public bool IsSpeaking => TTS_IsSpeaking();

        public IosSpeechService()
        {
            TTS_Init();
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            TTS_Stop();
            TTS_Speak(text, _lang);
        }

        public void StopAll()
        {
            TTS_Stop();
        }

        public void SetLanguage(string languageCode)
        {
            _lang = languageCode switch
            {
                "pl" => "pl-PL",
                "en" => "en-US",
                _ => "en-US"
            };
        }
    }
}
#endif