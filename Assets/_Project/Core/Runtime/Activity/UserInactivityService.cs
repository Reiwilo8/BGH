using UnityEngine;

namespace Project.Core.Activity
{
    public sealed class UserInactivityService : IUserInactivityService
    {
        private float _lastNavTime;

        public UserInactivityService()
        {
            _lastNavTime = Time.unscaledTime;
        }

        public float SecondsSinceLastNavAction => Mathf.Max(0f, Time.unscaledTime - _lastNavTime);

        public void MarkNavAction()
        {
            _lastNavTime = Time.unscaledTime;
        }

        public bool IsIdle(float thresholdSeconds) => SecondsSinceLastNavAction >= thresholdSeconds;
    }
}