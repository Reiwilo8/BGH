namespace Project.Core.Speech
{
    public interface ISpeechService
    {
        void Speak(string text, SpeechPriority priority = SpeechPriority.Normal);
        void StopAll();
        void SetLanguage(string languageCode);
        bool IsSpeaking { get; }
    }
}