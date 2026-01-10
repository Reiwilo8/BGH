#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class AndroidSpeechService : ISpeechService
    {
        private AndroidJavaObject _tts;
        private bool _ready;
        private string _lang = "en";

        private string _pendingText;

        public bool IsSpeaking { get; private set; }

        public AndroidSpeechService()
        {
            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                _tts = new AndroidJavaObject(
                    "android.speech.tts.TextToSpeech",
                    activity,
                    new OnInitListener(status =>
                    {
                        _ready = (status == 0);

                        if (!_ready)
                        {
                            Debug.LogWarning("[AndroidTTS] Init failed (status != SUCCESS).");
                            return;
                        }

                        ApplyLanguage();

                        if (!string.IsNullOrWhiteSpace(_pendingText))
                        {
                            var text = _pendingText;
                            _pendingText = null;
                            SpeakInternal(text);
                        }
                    }));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidTTS] Init failed: {e}");
                _tts = null;
                _ready = false;
            }
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (_tts == null)
            {
                Debug.Log($"[AndroidTTS:FALLBACK] {text}");
                return;
            }

            if (!_ready)
            {
                _pendingText = text;
                Debug.Log($"[AndroidTTS] Not ready yet, queued pending text: {text}");
                return;
            }

            SpeakInternal(text);
        }

        public void StopAll()
        {
            if (_tts == null) return;

            try
            {
                _pendingText = null;
                _tts.Call<int>("stop");
            }
            catch { }

            IsSpeaking = false;
        }

        public void SetLanguage(string languageCode)
        {
            _lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;

            if (_ready)
                ApplyLanguage();
        }

        private void SpeakInternal(string text)
        {
            try
            {
                IsSpeaking = true;

                _tts.Call<int>("speak", text, 0, null, "utt");

                IsSpeaking = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidTTS] Speak failed: {e}");
                Debug.Log($"[AndroidTTS:FALLBACK] {text}");
                IsSpeaking = false;
            }
        }

        private void ApplyLanguage()
        {
            if (_tts == null) return;

            try
            {
                var locale = new AndroidJavaObject("java.util.Locale", _lang);
                _tts.Call<int>("setLanguage", locale);
            }
            catch { }
        }

        private sealed class OnInitListener : AndroidJavaProxy
        {
            private readonly System.Action<int> _onInit;

            public OnInitListener(System.Action<int> onInit)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                _onInit = onInit;
            }

            public void onInit(int status) => _onInit?.Invoke(status);
        }
    }
}
#endif