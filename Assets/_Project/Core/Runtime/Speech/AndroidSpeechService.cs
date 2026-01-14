#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class AndroidSpeechService : ISpeechService
    {
        private AndroidJavaObject _tts;
        private bool _ready;
        private string _lang = "en";

        private string _pendingText;

        private volatile bool _isSpeaking;
        public bool IsSpeaking => _isSpeaking;

        private int _uttCounter;

        public AndroidSpeechService()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                _tts = new AndroidJavaObject(
                    "android.speech.tts.TextToSpeech",
                    activity,
                    new OnInitListener(OnTtsInit)
                );

                var listener = new UtteranceListener(
                    onStart: _ => _isSpeaking = true,
                    onDone: _ => _isSpeaking = false,
                    onError: _ => _isSpeaking = false
                );

                _tts.Call("setOnUtteranceProgressListener", listener);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AndroidSpeechService init failed: {e.Message}");
                _tts = null;
                _ready = false;
            }
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!_ready || _tts == null)
            {
                _pendingText = text;
                return;
            }

            SpeakInternal(text);
        }

        public void StopAll()
        {
            try
            {
                _pendingText = null;
                _isSpeaking = false;

                if (_tts != null)
                    _tts.Call<int>("stop");
            }
            catch { }
        }

        public void SetLanguage(string languageCode)
        {
            _lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode;

            if (!_ready || _tts == null) return;

            try
            {
                AndroidJavaObject locale;
                if (_lang.Contains("-"))
                {
                    var parts = _lang.Split('-');
                    locale = new AndroidJavaObject("java.util.Locale", parts[0], parts[1]);
                }
                else
                {
                    locale = new AndroidJavaObject("java.util.Locale", _lang);
                }

                _tts.Call<int>("setLanguage", locale);
            }
            catch { }
        }

        private void OnTtsInit(int status)
        {
            _ready = (status == 0);

            if (!_ready || _tts == null)
                return;

            SetLanguage(_lang);

            if (!string.IsNullOrWhiteSpace(_pendingText))
            {
                var text = _pendingText;
                _pendingText = null;
                SpeakInternal(text);
            }
        }

        private void SpeakInternal(string text)
        {
            try
            {
                var utteranceId = $"utt_{++_uttCounter}";

                using var bundle = new AndroidJavaObject("android.os.Bundle");
                bundle.Call("putString", "utteranceId", utteranceId);

                _tts.Call<int>("speak", text, 0, bundle, utteranceId);
            }
            catch
            {
                _isSpeaking = false;
            }
        }

        private sealed class OnInitListener : AndroidJavaProxy
        {
            private readonly Action<int> _onInit;

            public OnInitListener(Action<int> onInit)
                : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                _onInit = onInit;
            }

            public void onInit(int status) => _onInit?.Invoke(status);
        }

        private sealed class UtteranceListener : AndroidJavaProxy
        {
            private readonly Action<string> _onStart;
            private readonly Action<string> _onDone;
            private readonly Action<string> _onError;

            public UtteranceListener(Action<string> onStart, Action<string> onDone, Action<string> onError)
                : base("android.speech.tts.UtteranceProgressListener")
            {
                _onStart = onStart;
                _onDone = onDone;
                _onError = onError;
            }

            public void onStart(string utteranceId) => _onStart?.Invoke(utteranceId);
            public void onDone(string utteranceId) => _onDone?.Invoke(utteranceId);

            public void onError(string utteranceId) => _onError?.Invoke(utteranceId);
            public void onError(string utteranceId, int errorCode) => _onError?.Invoke(utteranceId);
        }
    }
}
#endif