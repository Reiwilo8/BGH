using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class AndroidSpeechService : ISpeechService
    {
        public bool IsSpeaking { get; private set; }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Debug.Log($"[AndroidTTS STUB:{priority}] {text}");
        }

        public void StopAll() { }
        public void SetLanguage(string languageCode) { }
    }
}