#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Project.Core.Speech
{
    public sealed class WindowsSpeechService : ISpeechService
    {
        [DllImport("TtsBridgeWin")]
        private static extern bool TTS_Init();

        [DllImport("TtsBridgeWin", CharSet = CharSet.Unicode)]
        private static extern void TTS_Speak(string text);

        [DllImport("TtsBridgeWin")]
        private static extern void TTS_Stop();

        [DllImport("TtsBridgeWin")]
        private static extern void TTS_Shutdown();

        public bool IsSpeaking { get; private set; }

        public WindowsSpeechService()
        {
            TTS_Init();
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            IsSpeaking = true;
            TTS_Speak(text);
            IsSpeaking = false;
        }

        public void StopAll()
        {
            TTS_Stop();
            IsSpeaking = false;
        }

        public void SetLanguage(string languageCode)
        {
        }

        ~WindowsSpeechService()
        {
            TTS_Shutdown();
        }
    }
}
#endif