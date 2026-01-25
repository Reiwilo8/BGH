using Project.Core.App;
using UnityEngine;

namespace Project.Core.Activity
{
    public sealed class RepeatAutoDriver : MonoBehaviour
    {
        [SerializeField] private float checkIntervalSeconds = 0.5f;

        private float _next;
        private RepeatService _repeat;

        public void Init(float intervalSeconds)
        {
            checkIntervalSeconds = intervalSeconds;
        }

        private void Awake()
        {
            var s = AppContext.Services;
            var r = s.Resolve<IRepeatService>();
            _repeat = r as RepeatService;
        }

        private void Update()
        {
            if (_repeat == null) return;

            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Mathf.Max(0.05f, checkIntervalSeconds);

            _repeat.TickAuto();
        }
    }
}