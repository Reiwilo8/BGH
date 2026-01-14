using Project.Core.Activity;
using Project.Core.App;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Project.Core.Input.Gestures
{
    public sealed class TouchGestureInputSource : MonoBehaviour
    {
        [SerializeField] private GestureSettings settings;

        private IInputService _input;

        private bool _singleActive;
        private int _singleFingerId;
        private Vector2 _singleStartPos;
        private float _singleStartTime;
        private bool _longPressFired;

        private float _lastTapTime = -999f;
        private Vector2 _lastTapPos;

        private bool _twoActive;
        private int _fingerA, _fingerB;
        private Vector2 _aStart, _bStart;
        private float _twoStartTime;
        private float _aUpTime = -999f, _bUpTime = -999f;
        private Vector2 _aUpPos, _bUpPos;

        private float _twoStartDistance;
        private bool _twoCanceledByPinch;

        private IRepeatService _repeat;
        private Coroutine _pendingSingleTapCo;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("[Gestures] GestureSettings not assigned.");
                enabled = false;
                return;
            }

            _input = AppContext.Services.Resolve<IInputService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();

#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif

            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerMove += OnFingerMove;
            Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif

            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerMove -= OnFingerMove;
            Touch.onFingerUp -= OnFingerUp;
        }

        private float Now() => settings.useUnscaledTime ? Time.unscaledTime : Time.time;

        private void OnFingerDown(Finger finger)
        {
            if (Touch.activeFingers.Count >= 2)
            {
                _lastTapTime = -999f;
            }

            if (Touch.activeFingers.Count == 2 && !_twoActive)
            {
                StartTwoFinger();
                return;
            }

            if (_twoActive) return;

            if (Touch.activeFingers.Count == 1)
            {
                _singleActive = true;
                _singleFingerId = finger.index;
                _singleStartPos = finger.screenPosition;
                _singleStartTime = Now();
                _longPressFired = false;
            }
        }

        private void OnFingerMove(Finger finger)
        {
            if (_twoActive)
            {
                if (Touch.activeFingers.Count >= 2)
                {
                    var f0 = Touch.activeFingers[0].screenPosition;
                    var f1 = Touch.activeFingers[1].screenPosition;
                    float dist = Vector2.Distance(f0, f1);
                    if (Mathf.Abs(dist - _twoStartDistance) > settings.twoFingerMaxPinchDeltaPx)
                        _twoCanceledByPinch = true;
                }
                return;
            }

            if (!_singleActive) return;
            if (finger.index != _singleFingerId) return;

            if (_longPressFired) return;

            float held = Now() - _singleStartTime;
            if (held < settings.longPressTime) return;

            float move = Vector2.Distance(finger.screenPosition, _singleStartPos);
            if (move > settings.longPressMaxMovePx) return;

            _longPressFired = true;
            _input.Emit(NavAction.Back);
        }

        private void OnFingerUp(Finger finger)
        {
            if (_twoActive)
            {
                RegisterTwoFingerUp(finger);
                TryResolveTwoFingerTap();
                return;
            }

            if (!_singleActive) return;
            if (finger.index != _singleFingerId) return;

            if (_longPressFired)
            {
                CancelPendingSingleTap();
                _singleActive = false;
                return;
            }

            var endPos = finger.screenPosition;
            var endTime = Now();
            var dt = endTime - _singleStartTime;

            var delta = endPos - _singleStartPos;
            var dist = delta.magnitude;

            if (dt <= settings.swipeMaxTime && dist >= settings.swipeMinDistancePx)
            {
                CancelPendingSingleTap();
                EmitSwipe(delta);
                _singleActive = false;
                return;
            }

            if (dist <= settings.tapMaxMovePx)
            {
                bool isDouble =
                    (endTime - _lastTapTime) <= settings.doubleTapMaxDelay &&
                    Vector2.Distance(endPos, _lastTapPos) <= settings.doubleTapMaxGapPx;

                if (isDouble)
                {
                    CancelPendingSingleTap();

                    _lastTapTime = -999f;
                    _input.Emit(NavAction.Confirm);
                }
                else
                {
                    _lastTapTime = endTime;
                    _lastTapPos = endPos;

                    CancelPendingSingleTap();
                    _pendingSingleTapCo = StartCoroutine(FireSingleTapRepeatAfterDelay());
                }
            }

            _singleActive = false;
        }

        private void EmitSwipe(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                if (delta.x < 0) _input.Emit(NavAction.Next);
                else _input.Emit(NavAction.Previous);
            }
            else
            {
                if (delta.y > 0) _input.Emit(NavAction.Next);
                else _input.Emit(NavAction.Previous);
            }
        }

        private void StartTwoFinger()
        {
            _twoActive = true;

            var f0 = Touch.activeFingers[0];
            var f1 = Touch.activeFingers[1];

            _fingerA = f0.index;
            _fingerB = f1.index;

            _aStart = f0.screenPosition;
            _bStart = f1.screenPosition;

            _twoStartDistance = Vector2.Distance(_aStart, _bStart);
            _twoCanceledByPinch = false;

            _twoStartTime = Now();
            _aUpTime = _bUpTime = -999f;
        }

        private void RegisterTwoFingerUp(Finger finger)
        {
            float t = Now();

            if (finger.index == _fingerA)
            {
                _aUpTime = t;
                _aUpPos = finger.screenPosition;
            }
            else if (finger.index == _fingerB)
            {
                _bUpTime = t;
                _bUpPos = finger.screenPosition;
            }
        }

        private void TryResolveTwoFingerTap()
        {
            if (_aUpTime < 0 || _bUpTime < 0) return;

            float endTime = Mathf.Max(_aUpTime, _bUpTime);
            float duration = endTime - _twoStartTime;

            float upDelta = Mathf.Abs(_aUpTime - _bUpTime);

            bool timeOk = duration <= settings.twoFingerTapMaxTime && upDelta <= settings.twoFingerMaxUpDelta;

            bool moveOk =
                Vector2.Distance(_aStart, _aUpPos) <= settings.twoFingerMaxMovePx &&
                Vector2.Distance(_bStart, _bUpPos) <= settings.twoFingerMaxMovePx;

            if (!_twoCanceledByPinch && timeOk && moveOk)
            {
                _input.Emit(NavAction.ToggleVisualAssist);
            }

            _twoActive = false;
            _singleActive = false;
        }

        private void CancelPendingSingleTap()
        {
            if (_pendingSingleTapCo != null)
            {
                StopCoroutine(_pendingSingleTapCo);
                _pendingSingleTapCo = null;
            }
        }

        private System.Collections.IEnumerator FireSingleTapRepeatAfterDelay()
        {
            yield return new WaitForSecondsRealtime(settings.doubleTapMaxDelay);

            _pendingSingleTapCo = null;
            _repeat.RequestRepeat("singleTap");
        }
    }
}