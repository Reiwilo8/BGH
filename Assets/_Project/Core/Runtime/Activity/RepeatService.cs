using System;
using Project.Core.App;
using Project.Core.Speech;
using Project.Core.Settings;

namespace Project.Core.Activity
{
    public sealed class RepeatService : IRepeatService
    {
        private readonly IUserInactivityService _inactivity;
        private readonly ISpeechService _speech;
        private readonly IAppFlowService _flow;
        private readonly ISettingsService _settings;

        public float IdleThresholdSeconds { get; set; } = 10f;

        public event Action RepeatRequested;

        public RepeatService(
            IUserInactivityService inactivity,
            ISpeechService speech,
            IAppFlowService flow,
            ISettingsService settings)
        {
            _inactivity = inactivity;
            _speech = speech;
            _flow = flow;
            _settings = settings;
        }

        public void RequestRepeat(string source = null)
        {
            if (!CanRepeatNow(resetIdleIfBusy: true))
                return;

            if (!_inactivity.IsIdle(IdleThresholdSeconds))
                return;

            FireRepeat();
        }

        public void TickAuto()
        {
            if (_settings == null)
                return;

            var s = _settings.Current;
            if (!s.autoRepeatEnabled)
                return;

            if (RepeatRequested == null)
                return;

            if (!CanRepeatNow(resetIdleIfBusy: true))
                return;

            float autoDelay = s.autoRepeatIdleSeconds;

            if (autoDelay < IdleThresholdSeconds)
                autoDelay = IdleThresholdSeconds;

            if (!_inactivity.IsIdle(autoDelay))
                return;

            FireRepeat();
        }

        private bool CanRepeatNow(bool resetIdleIfBusy)
        {
            bool busy =
                (_speech != null && _speech.IsSpeaking) ||
                (_flow != null && _flow.IsTransitioning);

            if (busy)
            {
                if (resetIdleIfBusy)
                    _inactivity?.MarkNavAction();

                return false;
            }

            return true;
        }

        private void FireRepeat()
        {
            RepeatRequested?.Invoke();

            _inactivity?.MarkNavAction();
        }
    }
}