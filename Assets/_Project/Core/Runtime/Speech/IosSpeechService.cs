using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class IosSpeechService : ISpeechService
    {
        public bool IsSpeaking { get; private set; }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            IsSpeaking = true;
            Debug.Log($"[iOSTTS STUB:{priority}] {text}");
            IsSpeaking = false;
        }

        public void StopAll() { IsSpeaking = false; }
        public void SetLanguage(string languageCode) { }
    }
}