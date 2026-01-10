#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class IosSpeechService : ISpeechService
    {
        [DllImport("__Internal")] private static extern void TTS_Init();
        [DllImport("__Internal")] private static extern void TTS_Speak(string text, string lang);
        [DllImport("__Internal")] private static extern void TTS_Stop();

        private string _lang = "en-US";

        public bool IsSpeaking { get; private set; }

        public IosSpeechService()
        {
            try
            {
                TTS_Init();
            }
            catch
            {
                Debug.LogWarning("[iOSTTS] Init failed (expected outside iOS runtime).");
            }
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            IsSpeaking = true;

            TTS_Stop();
            TTS_Speak(text, _lang);

            IsSpeaking = false;
        }

        public void StopAll()
        {
            TTS_Stop();
            IsSpeaking = false;
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