using System;
using Project.Core.App;
using Project.Core.Speech;

namespace Project.Core.Activity
{
    public sealed class RepeatService : IRepeatService
    {
        private readonly IUserInactivityService _inactivity;
        private readonly ISpeechService _speech;
        private readonly IAppFlowService _flow;

        public float IdleThresholdSeconds { get; set; } = 10f;

        public event Action RepeatRequested;

        public RepeatService(IUserInactivityService inactivity, ISpeechService speech, IAppFlowService flow)
        {
            _inactivity = inactivity;
            _speech = speech;
            _flow = flow;
        }

        public void RequestRepeat(string source = null)
        {
            if (_speech.IsSpeaking) return;
            if (_flow.IsTransitioning) return;

            if (!_inactivity.IsIdle(IdleThresholdSeconds)) return;

            RepeatRequested?.Invoke();
        }
    }
}