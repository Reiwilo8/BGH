using Project.Core.Activity;
using Project.Core.App;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.UI.VisualAssist
{
    public sealed class VisualAssistIdleDriver : MonoBehaviour
    {
        [SerializeField] private float checkIntervalSeconds = 0.15f;

        private IVisualModeService _visualMode;
        private IVisualAssistService _va;
        private IUserInactivityService _inactivity;
        private ISpeechService _speech;
        private IAppFlowService _flow;
        private ISettingsService _settings;

        private float _nextCheck;

        public void Init(float intervalSeconds)
        {
            checkIntervalSeconds = intervalSeconds;
        }

        private void Awake()
        {
            var s = AppContext.Services;
            _visualMode = s.Resolve<IVisualModeService>();
            _va = s.Resolve<IVisualAssistService>();
            _inactivity = s.Resolve<IUserInactivityService>();
            _speech = s.Resolve<ISpeechService>();
            _flow = s.Resolve<IAppFlowService>();
            _settings = s.Resolve<ISettingsService>();
        }

        private void Update()
        {
            if (_va == null || _visualMode == null) return;
            if (_visualMode.Mode != VisualMode.VisualAssist) return;

            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + Mathf.Max(0.05f, checkIntervalSeconds);

            bool isSpeaking = _speech != null && _speech.IsSpeaking;
            bool isTransitioning = _flow != null && _flow.IsTransitioning;

            if (isSpeaking || isTransitioning)
            {
                _inactivity?.MarkNavAction();
                _va.EvaluateIdleHint(canShow: false, idleSeconds: 0f);
                return;
            }

            if (_settings == null || _settings.Current == null || _inactivity == null)
            {
                _va.EvaluateIdleHint(canShow: false, idleSeconds: 0f);
                return;
            }

            float threshold = Mathf.Clamp(_settings.Current.repeatIdleSeconds, 1f, 15f);
            bool canShow = _inactivity.IsIdle(threshold);

            _va.EvaluateIdleHint(
                canShow,
                _inactivity.SecondsSinceLastNavAction);
        }
    }
}