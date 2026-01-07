using UnityEngine;

namespace Project.Core.Input.Gestures
{
    [CreateAssetMenu(menuName = "Project/Input/Gesture Settings", fileName = "GestureSettings")]
    public sealed class GestureSettings : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Use unscaled time for gesture timing (recommended).")]
        public bool useUnscaledTime = true;

        [Header("Swipe")]
        [Tooltip("Minimum swipe distance in pixels.")]
        public float swipeMinDistancePx = 100f;

        [Tooltip("Max time (seconds) for swipe gesture.")]
        public float swipeMaxTime = 0.4f;

        [Header("Tap / Double Tap")]
        [Tooltip("Max movement (pixels) for a tap to still count as a tap.")]
        public float tapMaxMovePx = 32f;

        [Tooltip("Max delay (seconds) between taps to count as a double tap.")]
        public float doubleTapMaxDelay = 0.40f;

        [Tooltip("Max distance between two taps to count as a double tap.")]
        public float doubleTapMaxGapPx = 60f;

        [Header("Long Press")]
        [Tooltip("Hold time (seconds) to trigger long press.")]
        public float longPressTime = 0.6f;

        [Tooltip("Max movement (pixels) during hold to still count as long press.")]
        public float longPressMaxMovePx = 18f;

        [Header("Two-Finger (future-proof)")]
        [Tooltip("If distance between two fingers changes more than this, treat it as pinch/rotate candidate and do not trigger two-finger tap.")]
        public float twoFingerMaxPinchDeltaPx = 40f;

        [Header("Two-Finger Tap")]
        [Tooltip("Max time (seconds) between two-finger down and up to count as two-finger tap.")]
        public float twoFingerTapMaxTime = 0.28f;

        [Tooltip("Max movement (pixels) allowed for each finger during two-finger tap.")]
        public float twoFingerMaxMovePx = 22f;

        [Tooltip("Max time difference (seconds) between both finger ups.")]
        public float twoFingerMaxUpDelta = 0.14f;
    }
}