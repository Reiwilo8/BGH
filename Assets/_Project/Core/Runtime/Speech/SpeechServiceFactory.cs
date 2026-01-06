namespace Project.Core.Speech
{
    public static class SpeechServiceFactory
    {
        public static ISpeechService Create(ISpeechFeed feed = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidSpeechService();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IosSpeechService();
#else
            return new EditorSpeechService(feed);
#endif
        }
    }
}