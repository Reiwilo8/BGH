using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class EditorSpeechService : ISpeechService
    {
        public bool IsSpeaking { get; private set; }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            IsSpeaking = true;
            Debug.Log($"[TTS:{priority}] {text}");
            IsSpeaking = false;
        }

        public void StopAll()
        {
            Debug.Log("[TTS] StopAll()");
            IsSpeaking = false;
        }

        public void SetLanguage(string languageCode)
        {
            Debug.Log($"[TTS] SetLanguage({languageCode})");
        }
    }
}