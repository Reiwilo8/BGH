namespace Project.Core.Speech
{
    public interface ISpeechLocalizer
    {
        void SpeakKey(string key, SpeechPriority priority = SpeechPriority.Normal);
        void SpeakKey(string key, SpeechPriority priority, params object[] args);
        void StopAll();
    }
}