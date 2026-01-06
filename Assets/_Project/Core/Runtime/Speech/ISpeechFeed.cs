namespace Project.Core.Speech
{
    public interface ISpeechFeed
    {
        void OnSpoken(string text, SpeechPriority priority);
    }
}