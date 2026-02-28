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

        private float _neutralTiltDeg;

        private enum TiltState { None, Left, Right }
        private TiltState _tiltState = TiltState.None;

        private float _nextTiltEmitTime;

#if ENABLE_INPUT_SYSTEM
        private Accelerometer _accelerometer;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidAccelListener _androidListener;
        private AndroidJavaObject _androidSensorService;
        private Vector3 _androidAccMs2;
        private bool _androidHasAcc;
#endif

        private const float Ms2PerG = 9.80665f;

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
            _accelerometer = Accelerometer.current;
            if (_accelerometer == null)
            {
                try { _accelerometer = InputSystem.AddDevice<Accelerometer>(); }
                catch { _accelerometer = Accelerometer.current; }
            }
            if (_accelerometer != null) InputSystem.EnableDevice(_accelerometer);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            if (settings.useAndroidSensorManagerFallback)
                TryStartAndroidAccel();
#endif
        }

        private void OnDisable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            TryStopAndroidAccel();
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID || UNITY_IOS
            float now = Now();
            float dt = DeltaTime();

            bool haveAccG = TryReadAccelerometerG(out Vector3 accG);

            if (!haveAccG)
                haveAccG = TryReadLegacyAccelerationG(out accG);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!haveAccG && settings.useAndroidSensorManagerFallback)
            {
                if (TryReadAndroidAccelerometerG(out Vector3 aG))
                {
                    accG = aG;
                    haveAccG = true;
                }
            }
#endif

            if (!haveAccG)
                return;

            float tiltLerpT = 1f - settings.gravityLowPass;
            _gravity = Vector3.Lerp(_gravity, accG, tiltLerpT);

            UpdateTilt(now, dt);
#endif
        }

        private float Now() => settings.useUnscaledTime ? Time.unscaledTime : Time.time;
        private float DeltaTime() => settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

#if ENABLE_INPUT_SYSTEM
        private bool TryReadAccelerometerG(out Vector3 g)
        {
            g = default;
            if (_accelerometer == null) return false;

            Vector3 ms2 = _accelerometer.acceleration.ReadValue();
            if (ms2.sqrMagnitude < 0.000001f) return false;

            g = ms2 / Ms2PerG;
            return true;
        }
#else
        private bool TryReadAccelerometerG(out Vector3 g) { g = default; return false; }
#endif

        private bool TryReadLegacyAccelerationG(out Vector3 g)
        {
            g = default;
#if UNITY_ANDROID || UNITY_IOS
            Vector3 a = UnityEngine.Input.acceleration;
            if (a.sqrMagnitude < 0.000001f) return false;
            g = a;
            return true;
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool TryReadAndroidAccelerometerG(out Vector3 g)
        {
            g = default;
            if (!_androidHasAcc) return false;
            Vector3 ms2 = _androidAccMs2;
            if (ms2.sqrMagnitude < 0.000001f) return false;
            g = ms2 / Ms2PerG;
            return true;
        }

        private void TryStartAndroidAccel()
        {
            if (_androidListener != null) return;

            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) return;

                using var contextClass = new AndroidJavaClass("android.content.Context");
                string sensorServiceName = contextClass.GetStatic<string>("SENSOR_SERVICE");
                var sensorService = activity.Call<AndroidJavaObject>("getSystemService", sensorServiceName);
                if (sensorService == null) return;

                int sensorTypeAccelerometer = 1;
                var accel = sensorService.Call<AndroidJavaObject>("getDefaultSensor", sensorTypeAccelerometer);
                if (accel == null) return;

                _androidListener = new AndroidAccelListener(this);
                int delayGame = 1;

                bool ok = sensorService.Call<bool>("registerListener", _androidListener, accel, delayGame);
                if (!ok)
                {
                    _androidListener = null;
                    return;
                }

                _androidSensorService = sensorService;
                _androidHasAcc = true;
            }
            catch
            {
                _androidListener = null;
                _androidSensorService = null;
                _androidHasAcc = false;
            }
        }

        private void TryStopAndroidAccel()
        {
            if (_androidListener == null) return;

            try
            {
                if (_androidSensorService != null)
                    _androidSensorService.Call("unregisterListener", _androidListener);
            }
            catch { }

            _androidListener = null;
            _androidSensorService = null;
            _androidHasAcc = false;
        }

        private sealed class AndroidAccelListener : AndroidJavaProxy
        {
            private readonly MotionInputSource _owner;

            public AndroidAccelListener(MotionInputSource owner)
                : base("android.hardware.SensorEventListener")
            {
                _owner = owner;
            }

            void onSensorChanged(AndroidJavaObject eventObj)
            {
                try
                {
                    float[] values = eventObj.Get<float[]>("values");
                    if (values == null || values.Length < 3) return;

                    _owner._androidAccMs2 = new Vector3(values[0], values[1], values[2]);
                    _owner._androidHasAcc = true;
                }
                catch { }
            }

            void onAccuracyChanged(AndroidJavaObject sensor, int accuracy) { }
        }
#endif

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