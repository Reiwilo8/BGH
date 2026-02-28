using UnityEngine;

namespace Project.Core.Input.Motion
{
    [CreateAssetMenu(menuName = "Project/Input/Motion Input Settings", fileName = "MotionInputSettings")]
    public sealed class MotionInputSettings : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Use unscaled time for motion input timing.")]
        public bool useUnscaledTime = true;

        [Header("Gravity filter (tilt)")]
        [Tooltip("Low-pass smoothing for gravity vector. Higher = more stable, slower response.")]
        [Range(0.80f, 0.999f)]
        public float gravityLowPass = 0.94f;

        [Header("Tilt (Steering Wheel / Landscape)")]
        [Tooltip("Deadzone around neutral (degrees). Inside: no tilt emitted.")]
        [Range(0f, 25f)]
        public float tiltDeadZoneDeg = 4.0f;

        [Tooltip("Threshold to enter tilt state (degrees).")]
        [Range(0f, 45f)]
        public float tiltOnDeg = 12.0f;

        [Tooltip("Threshold to exit tilt state (degrees). Must be < tiltOnDeg.")]
        [Range(0f, 45f)]
        public float tiltOffDeg = 8.0f;

        [Tooltip("Continuous emit rate while tilting (Hz).")]
        [Range(1f, 30f)]
        public float tiltEmitRateHz = 10f;

        [Header("Auto-neutral baseline")]
        [Tooltip("How fast neutral baseline follows the current value when close to neutral.")]
        [Range(0f, 10f)]
        public float neutralFollowSpeed = 1.8f;

        [Tooltip("Baseline updates only if |tilt| is smaller than this (degrees).")]
        [Range(0f, 25f)]
        public float neutralFollowMaxAbsDeg = 10f;

        [Header("Optional gating by orientation")]
        [Tooltip("If true, tilt emits only in Landscape. Shake still works.")]
        public bool tiltOnlyInLandscape = true;

        [Header("Shake (Moderate)")]
        [Tooltip("Linear acceleration peak threshold (in g).")]
        [Range(0.5f, 5f)]
        public float shakePeakThresholdG = 0.95f;

        [Tooltip("How many peaks must occur within shakeWindowSeconds to trigger shake.")]
        [Range(2, 6)]
        public int shakeRequiredPeaks = 2;

        [Tooltip("Time window for collecting shake peaks (seconds).")]
        [Range(0.10f, 1.0f)]
        public float shakeWindowSeconds = 0.65f;

        [Tooltip("Minimum time between peaks (seconds).")]
        [Range(0.01f, 0.30f)]
        public float shakeMinPeakGapSeconds = 0.08f;

        [Tooltip("Cooldown after shake fires (seconds).")]
        [Range(0.10f, 2.0f)]
        public float shakeCooldownSeconds = 0.45f;

        [Header("Shake fallback (guaranteed)")]
        [Tooltip("If sensors are unavailable or return zeros, allow double-tap to trigger Shake.")]
        public bool shakeFallbackEnable = true;

        [Tooltip("Max gap between two taps to count as Shake fallback.")]
        [Range(0.10f, 0.60f)]
        public float shakeFallbackDoubleTapMaxGapSeconds = 0.30f;

        [Header("Hard fallback (Android SensorManager)")]
        [Tooltip("If true, MotionInputSource may read accelerometer via Android SensorManager when Input System sensors are unavailable.")]
        public bool useAndroidSensorManagerFallback = true;

        [Header("Routing sanity check (temporary)")]
        [Tooltip("If true, MotionInputSource emits a single MotionAction.Shake shortly after enable to verify routing end-to-end.")]
        public bool debugForceShakeOnEnable = false;

        [Tooltip("Delay before the forced shake emission (seconds).")]
        [Range(0.05f, 2.0f)]
        public float debugForceShakeDelaySeconds = 0.35f;
    }
}