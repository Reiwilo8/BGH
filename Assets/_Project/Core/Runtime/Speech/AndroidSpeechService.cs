#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class AndroidSpeechService : ISpeechService
    {
        private const string UnityGoName = "AndroidTtsBridgeReceiver";
        private const string UnityCallback = "OnTtsEvent";

        private AndroidJavaObject _bridge;

        private AndroidTtsBridgeReceiver _receiver;

        private string _lang = "en";
        private string _pendingText;

        private volatile int _activeAny;
        public bool IsSpeaking => _activeAny > 0;

        public AndroidSpeechService()
        {
            try
            {
                EnsureReceiver();

                using var cls = new AndroidJavaClass("com.project.core.speech.TtsBridgeAndroid");
                _bridge = cls.CallStatic<AndroidJavaObject>("create", UnityGoName, UnityCallback);

                TrySetLanguageNow();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AndroidSpeechService init failed: {e.Message}");
                _bridge = null;
                _activeAny = 0;
            }
        }

        public void Speak(string text, SpeechPriority priority = SpeechPriority.Normal)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (_bridge == null) return;

            if (!IsReadySafe())
            {
                _pendingText = text;
                return;
            }

            SpeakInternal(text);
        }

        public void StopAll()
        {
            _pendingText = null;
            _activeAny = 0;

            try { _bridge?.Call("stopAll"); } catch { }
        }

        public void SetLanguage(string languageCode)
        {
            _lang = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
            TrySetLanguageNow();
        }

        private void EnsureReceiver()
        {
            var go = GameObject.Find(UnityGoName);
            if (go == null)
            {
                go = new GameObject(UnityGoName);
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            _receiver = go.GetComponent<AndroidTtsBridgeReceiver>();
            if (_receiver == null)
                _receiver = go.AddComponent<AndroidTtsBridgeReceiver>();

            _receiver.OnEvent -= HandleBridgeEvent;
            _receiver.OnEvent += HandleBridgeEvent;
        }

        private void HandleBridgeEvent(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            int sep = msg.IndexOf('|');
            string kind = sep >= 0 ? msg.Substring(0, sep) : msg;

            if (kind == "start_any")
            {
                _activeAny = 1;
                return;
            }

            if (kind == "done_any" || kind == "stop")
            {
                _activeAny = 0;
                return;
            }

            if (kind == "ready")
            {
                TrySetLanguageNow();

                if (!string.IsNullOrWhiteSpace(_pendingText))
                {
                    var t = _pendingText;
                    _pendingText = null;
                    SpeakInternal(t);
                }

                return;
            }
        }

        private bool IsReadySafe()
        {
            try { return _bridge != null && _bridge.Call<bool>("isReady"); }
            catch { return false; }
        }

        private void TrySetLanguageNow()
        {
            if (_bridge == null) return;

            try
            {
                if (IsReadySafe())
                    _bridge.Call("setLanguage", _lang);
            }
            catch { }
        }

        private void SpeakInternal(string text)
        {
            try
            {
                _bridge.Call<string>("speak", text, true);
            }
            catch
            {
                _activeAny = 0;
            }
        }
    }
}
#endif