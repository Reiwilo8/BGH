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

        [DllImport("TtsBridgeWin", CharSet = CharSet.Unicode)]
        private static extern void TTS_SetLanguage(string lang);

        [DllImport("TtsBridgeWin")]
        private static extern void TTS_Shutdown();

        [DllImport("TtsBridgeWin")]
        private static extern bool TTS_IsSpeaking();

        private string _lang = "en";

        public bool IsSpeaking => TTS_IsSpeaking();

        public WindowsSpeechService()
        {
            TTS_Init();
            TTS_SetLanguage(_lang);
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            TTS_Speak(text);
        }

        public void StopAll()
        {
            TTS_Stop();
        }

        public void SetLanguage(string languageCode)
        {
            _lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;
            TTS_SetLanguage(_lang);
        }

        ~WindowsSpeechService()
        {
            TTS_Shutdown();
        }
    }
}
#endif