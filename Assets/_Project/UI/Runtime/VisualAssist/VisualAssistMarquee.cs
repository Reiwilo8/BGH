using System;
using TMPro;
using UnityEngine;

namespace Project.UI.VisualAssist
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class VisualAssistMarquee : MonoBehaviour
    {
        [Header("Auto tuning (recommended)")]
        [SerializeField] private bool autoTuneByPlatform = true;

        [Header("Timing (base)")]
        [SerializeField] private float targetCycleSeconds = 3.8f;

        [Tooltip("Never faster than this cycle (even for huge texts).")]
        [SerializeField] private float minCycleSeconds = 1.6f;

        [Tooltip("Never slower than this cycle (even for small overflow).")]
        [SerializeField] private float maxCycleSeconds = 4.2f;

        [Header("Dynamic speed tuning")]
        [Tooltip("Reference font size for 1.0 scale. Used to speed up marquee when Center font is larger.")]
        [SerializeField] private float baseFontSize = 36f;

        [Tooltip("How strongly we speed up when text is wider than viewport. 0 = off.")]
        [SerializeField] private float ratioBoostStrength = 0.85f;

        [Tooltip("How strongly we speed up when font size is larger than baseFontSize. 0 = off.")]
        [SerializeField] private float fontBoostStrength = 0.55f;

        [Tooltip("Cap for font-based boost (prevents too fast motion / smear). 1.25 = +25% speed max from font.")]
        [SerializeField] private float maxFontScaleBoost = 1.25f;

        [Header("User speed scale (from settings)")]
        [Tooltip("Final multiplier for marquee speed. 1.0 = default. >1 faster, <1 slower.")]
        [SerializeField] private float userSpeedScale = 1.0f;

        public float UserSpeedScale
        {
            get => userSpeedScale;
            set => userSpeedScale = Mathf.Clamp(value, 0.5f, 2.0f);
        }

        [Header("Clamp (px/s)")]
        [SerializeField] private float minSpeedPxPerSecond = 90f;
        [SerializeField] private float maxSpeedPxPerSecond = 360f;

        [Header("Layout")]
        [SerializeField] private float gapPx = 60f;

        [Header("Pauses")]
        [SerializeField] private float startPauseSeconds = 0.35f;
        [SerializeField] private float restartPauseSeconds = 0.25f;

        [Header("Quality")]
        [SerializeField] private bool pixelSnap = true;

        public event Action FirstFullPassCompleted;

        public bool IsActive => _active;

        private TMP_Text _text;
        private RectTransform _rt;
        private RectTransform _parent;

        private bool _active;
        private float _textWidth;
        private float _viewportWidth;
        private float _x;
        private float _speed;
        private float _pause;

        private string _lastText = "";
        private float _lastFontSize = -1f;
        private float _lastViewportWidth = -1f;
        private float _lastUserSpeedScale = -1f;

        private bool _firstPassPending;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _rt = GetComponent<RectTransform>();
            _parent = _rt.parent as RectTransform;

            if (autoTuneByPlatform)
                ApplyPlatformDefaults();
        }

        private void OnEnable()
        {
            Refresh(forceRestart: true);
        }

        public void Refresh(bool forceRestart = true)
        {
            if (_text == null || _rt == null || _parent == null)
                return;

            string current = _text.text ?? "";

            float fontSize = _text.fontSize;
            float viewportW = _parent.rect.width;
            float speedScale = Mathf.Clamp(UserSpeedScale, 0.5f, 2.0f);

            bool needRestart =
                forceRestart ||
                current != _lastText ||
                !Mathf.Approximately(fontSize, _lastFontSize) ||
                !Mathf.Approximately(viewportW, _lastViewportWidth) ||
                !Mathf.Approximately(speedScale, _lastUserSpeedScale);

            if (!needRestart)
                return;

            _lastText = current;
            _lastFontSize = fontSize;
            _lastViewportWidth = viewportW;
            _lastUserSpeedScale = speedScale;

            _text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            _textWidth = Mathf.Max(0f, _text.preferredWidth);
            _viewportWidth = Mathf.Max(0f, viewportW);

            _active = !string.IsNullOrWhiteSpace(current) && _textWidth > _viewportWidth;

            if (_active)
            {
                _firstPassPending = true;

                float distance = OffRightX() - OffLeftX();

                float ratio = (_viewportWidth > 1f) ? (_textWidth / _viewportWidth) : 2f;
                ratio = Mathf.Max(1f, ratio);

                float ratioBoost = 1f + ratioBoostStrength * (ratio - 1f);
                ratioBoost = Mathf.Max(1f, ratioBoost);

                float safeBaseFont = Mathf.Max(1f, baseFontSize);
                float fontScale = Mathf.Max(0.75f, fontSize / safeBaseFont);
                float fontBoost = 1f + fontBoostStrength * (fontScale - 1f);
                fontBoost = Mathf.Clamp(fontBoost, 1f, maxFontScaleBoost);

                float boostedCycle = targetCycleSeconds / (ratioBoost * fontBoost);
                float cycle = Mathf.Clamp(boostedCycle, minCycleSeconds, maxCycleSeconds);
                cycle = Mathf.Max(0.75f, cycle);

                _speed = Mathf.Clamp(distance / cycle, minSpeedPxPerSecond, maxSpeedPxPerSecond);

                _speed *= speedScale;

                _x = OffRightX();
                _pause = startPauseSeconds;

                SetX(_x);
            }
            else
            {
                _firstPassPending = false;

                _speed = 0f;
                _pause = 0f;
                SetX(0f);
            }
        }

        private void Update()
        {
            if (!_active) return;

            float dt = Time.unscaledDeltaTime;

            if (_pause > 0f)
            {
                _pause -= dt;
                return;
            }

            _x -= _speed * dt;

            if (_x <= OffLeftX())
            {
                if (_firstPassPending)
                {
                    _firstPassPending = false;
                    FirstFullPassCompleted?.Invoke();
                }

                _x = OffRightX();
                _pause = restartPauseSeconds;
            }

            SetX(_x);
        }

        private float OffRightX()
            => (_viewportWidth * 0.5f) + (_textWidth * 0.5f) + gapPx;

        private float OffLeftX()
            => -((_viewportWidth * 0.5f) + (_textWidth * 0.5f) + gapPx);

        private void SetX(float x)
        {
            if (pixelSnap) x = Mathf.Round(x);

            var p = _rt.anchoredPosition;
            p.x = x;
            _rt.anchoredPosition = p;
        }

        private void ApplyPlatformDefaults()
        {
            if (Application.isMobilePlatform)
            {
                targetCycleSeconds = 3.4f;
                minCycleSeconds = 1.45f;
                maxCycleSeconds = 4.0f;

                minSpeedPxPerSecond = 110f;
                maxSpeedPxPerSecond = 420f;

                ratioBoostStrength = 0.95f;
                fontBoostStrength = 0.60f;

                gapPx = Mathf.Max(gapPx, 70f);
            }
            else
            {
                targetCycleSeconds = 3.8f;
                minCycleSeconds = 1.6f;
                maxCycleSeconds = 4.2f;

                minSpeedPxPerSecond = 90f;
                maxSpeedPxPerSecond = 360f;

                ratioBoostStrength = 0.85f;
                fontBoostStrength = 0.55f;

                gapPx = Mathf.Max(gapPx, 60f);
            }
        }
    }
}