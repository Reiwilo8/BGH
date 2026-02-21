using System.Collections.Generic;
using Project.Core.App;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Core.Input.Motion
{
    public sealed class MotionInputSource : MonoBehaviour
    {
        [SerializeField] private MotionInputSettings settings;

        private IInputService _input;

        private Vector3 _gravity;
        private Vector3 _linear;

        private float _neutralTiltDeg;

        private enum TiltState { None, Left, Right }
        private TiltState _tiltState = TiltState.None;

        private float _nextTiltEmitTime;

        private readonly Queue<float> _shakePeaks = new Queue<float>();
        private float _lastPeakTime = -999f;
        private float _shakeCooldownUntil = -999f;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("[MotionInputSource] MotionInputSettings not assigned.");
                enabled = false;
                return;
            }

            _input = AppContext.Services.Resolve<IInputService>();
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (Accelerometer.current != null)
                InputSystem.EnableDevice(Accelerometer.current);
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID || UNITY_IOS
            float now = Now();
            float dt = DeltaTime();

            Vector3 acc = ReadAccelerationG(); // includes gravity, in g

            // Gravity low-pass
            float lerpT = 1f - settings.gravityLowPass;
            _gravity = Vector3.Lerp(_gravity, acc, lerpT);

            // Linear motion high-pass
            _linear = acc - _gravity;

            UpdateTilt(now, dt);
            UpdateShake(now);
#endif
        }

        private float Now() => settings.useUnscaledTime ? Time.unscaledTime : Time.time;
        private float DeltaTime() => settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private Vector3 ReadAccelerationG()
        {
#if ENABLE_INPUT_SYSTEM
            if (Accelerometer.current != null)
                return Accelerometer.current.acceleration.ReadValue();
#endif
            return UnityEngine.Input.acceleration;
        }

        private void UpdateTilt(float now, float dt)
        {
            if (settings.tiltOnlyInLandscape &&
                Screen.orientation != ScreenOrientation.LandscapeLeft &&
                Screen.orientation != ScreenOrientation.LandscapeRight)
            {
                _tiltState = TiltState.None;
                return;
            }

            float rollDeg = ComputeScreenRollDeg(_gravity, Screen.orientation);

            float foldedTiltDeg = FoldRollToSteeringTiltDeg(rollDeg);

            float centered = foldedTiltDeg - _neutralTiltDeg;

            if (Mathf.Abs(centered) <= settings.neutralFollowMaxAbsDeg)
            {
                float t = 1f - Mathf.Exp(-settings.neutralFollowSpeed * Mathf.Max(0f, dt));
                _neutralTiltDeg = Mathf.Lerp(_neutralTiltDeg, foldedTiltDeg, t);
                centered = foldedTiltDeg - _neutralTiltDeg;
            }

            if (Mathf.Abs(centered) <= settings.tiltDeadZoneDeg)
            {
                _tiltState = TiltState.None;
                return;
            }

            switch (_tiltState)
            {
                case TiltState.None:
                    if (centered >= settings.tiltOnDeg) _tiltState = TiltState.Right;
                    else if (centered <= -settings.tiltOnDeg) _tiltState = TiltState.Left;
                    break;

                case TiltState.Right:
                    if (centered <= settings.tiltOffDeg) _tiltState = TiltState.None;
                    break;

                case TiltState.Left:
                    if (centered >= -settings.tiltOffDeg) _tiltState = TiltState.None;
                    break;
            }

            if (_tiltState == TiltState.None)
                return;

            float interval = settings.tiltEmitRateHz <= 0.01f ? 0.1f : (1f / settings.tiltEmitRateHz);
            if (now < _nextTiltEmitTime)
                return;

            _nextTiltEmitTime = now + interval;

            if (_tiltState == TiltState.Left)
                _input.EmitMotion(MotionAction.TiltLeft);
            else
                _input.EmitMotion(MotionAction.TiltRight);
        }

        private void UpdateShake(float now)
        {
            if (now < _shakeCooldownUntil)
                return;

            float mag = _linear.magnitude;

            if (mag >= settings.shakePeakThresholdG &&
                (now - _lastPeakTime) >= settings.shakeMinPeakGapSeconds)
            {
                _lastPeakTime = now;
                _shakePeaks.Enqueue(now);
            }

            while (_shakePeaks.Count > 0 && (now - _shakePeaks.Peek()) > settings.shakeWindowSeconds)
                _shakePeaks.Dequeue();

            if (_shakePeaks.Count >= settings.shakeRequiredPeaks)
            {
                _shakePeaks.Clear();
                _shakeCooldownUntil = now + settings.shakeCooldownSeconds;
                _input.EmitMotion(MotionAction.Shake);
            }
        }

        private static float ComputeScreenRollDeg(Vector3 g, ScreenOrientation orientation)
        {
            float sx, sy;

            switch (orientation)
            {
                case ScreenOrientation.LandscapeLeft:
                    sx = g.y;
                    sy = -g.x;
                    break;

                case ScreenOrientation.LandscapeRight:
                    sx = -g.y;
                    sy = g.x;
                    break;

                case ScreenOrientation.PortraitUpsideDown:
                    sx = -g.x;
                    sy = -g.y;
                    break;

                case ScreenOrientation.Portrait:
                default:
                    sx = g.x;
                    sy = g.y;
                    break;
            }

            float rollRad = Mathf.Atan2(sx, sy);
            float rollDeg = rollRad * Mathf.Rad2Deg;

            rollDeg = NormalizeAngleDeg(rollDeg);
            return rollDeg;
        }

        private static float FoldRollToSteeringTiltDeg(float rollDeg)
        {
            float a = NormalizeAngleDeg(rollDeg);

            if (a > 90f) a = 180f - a;
            else if (a < -90f) a = -180f - a;

            return a;
        }

        private static float NormalizeAngleDeg(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }
    }
}