using Project.Core.App;
using Project.Core.Settings;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.VisualAssist
{
    public sealed class VisualAssistOverlayView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image dimmerPanel;

        [Header("Text")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text subHeaderText;
        [SerializeField] private TMP_Text centerText;

        [Header("Text Color (All)")]
        [SerializeField] private bool overrideAllTextColors = true;
        [SerializeField] private Color overlayFontColor = new Color(1f, 0.92f, 0.35f, 1f);

        [Header("Dimmer")]
        [SerializeField] private bool smoothDimmer = true;
        [SerializeField] private float dimmerLerpSpeed = 12f;

        [Header("Transition Fade (Header/SubHeader)")]
        [SerializeField] private bool transitionFade = true;
        [SerializeField] private float fadeOutSeconds = 0.18f;
        [SerializeField] private float fadeInSeconds = 0.22f;

        [Header("SubHeader Swipe (List Move)")]
        [SerializeField] private bool enableSubHeaderSwipe = true;

        [Tooltip("Target swipe speed. Higher = faster + smoother feeling under spam.")]
        [SerializeField] private float swipeSpeedPxPerSecond = 1800f;

        [Tooltip("Never go faster than this (prevents teleport feel on tiny distances).")]
        [SerializeField] private float swipeMinDurationSeconds = 0.06f;

        [Tooltip("Never go slower than this (keeps responsiveness even for long distances).")]
        [SerializeField] private float swipeMaxDurationSeconds = 0.14f;

        [SerializeField] private float swipeExtraGapPx = 40f;

        [Tooltip("How much of text width we consider for offscreen distance. 1 = full width. Lower = shorter travel (faster).")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float offscreenTextWidthFactor = 1.0f;

        [Header("Swipe Robustness")]
        [Tooltip("If user flips direction quickly (next<->prev), we 'snap back' to center before starting the opposite swipe. Keeps it predictable.")]
        [SerializeField] private float oppositePulseWindowSeconds = 0.08f;

        [Header("Typography (fallback if settings not available)")]
        [SerializeField] private VisualAssistTextSizePreset textSizePreset = VisualAssistTextSizePreset.Medium;

        private IVisualAssistService _va;
        private IVisualModeService _mode;
        private ISettingsService _settings;

        private IVisualAssistMarqueeGate _marqueeGate;

        private VisualMode _lastMode = (VisualMode)(-1);
        private float _currentDim;

        private VisualAssistTypographyProfile _typo;
        private RectTransform _rootRt;

        private VisualAssistMarquee _marquee;

        private RectTransform _subRt;
        private RectTransform _subParentRt;

        private Coroutine _swipeCo;
        private int _lastPulse;

        private string _lastStableSubText = "";
        private string _latestSubFromVa = "";

        private bool _lastTransitioning;

        private float _lastSwipeStartTime = -999f;
        private VaListMoveDirection _lastSwipeDir = VaListMoveDirection.None;

        private Coroutine _fadeCo;
        private enum FadePhase { None, Out, In }
        private FadePhase _fadePhase = FadePhase.None;
        private bool _pendingFadeInAfterOut;

        private float _fadeAlpha01 = 1f;
        private Color _headerBaseColor;
        private Color _subHeaderBaseColor;
        private bool _baseColorsCaptured;

        private Vector2 _lastRootSize = Vector2.zero;

        private VisualAssistTextSizePreset _appliedPreset = (VisualAssistTextSizePreset)(-1);
        private float _appliedDimmerStrength01 = -1f;
        private float _appliedMarqueeSpeedScale = -1f;

        private string _armedGateText = "";

        private void Awake()
        {
            var services = AppContext.Services;

            _va = services.Resolve<IVisualAssistService>();
            _mode = services.Resolve<IVisualModeService>();
            _settings = services.Resolve<ISettingsService>();

            _marqueeGate = _va as IVisualAssistMarqueeGate;

            _marquee = centerText != null ? centerText.GetComponent<VisualAssistMarquee>() : null;

            if (_marquee != null)
                _marquee.FirstFullPassCompleted += OnMarqueeFirstPassCompleted;

            if (_va != null)
                _va.Changed += RenderNow;

            if (dimmerPanel != null)
                dimmerPanel.raycastTarget = false;

            _subRt = subHeaderText != null ? subHeaderText.rectTransform : null;
            _subParentRt = _subRt != null ? _subRt.parent as RectTransform : null;

            _rootRt = root != null ? root.GetComponent<RectTransform>() : null;

            _typo = VisualAssistTypographyPresets.Get(textSizePreset);

            CaptureBaseTextColors();
            ApplyAllTextColor();
        }

        private void OnDestroy()
        {
            if (_marquee != null)
                _marquee.FirstFullPassCompleted -= OnMarqueeFirstPassCompleted;

            if (_va != null)
                _va.Changed -= RenderNow;

            ReleaseMarqueeGate();
        }

        private void OnEnable()
        {
            SyncFromSettings(force: true);
            ApplyAllTextColor();

            RenderNow();
            ApplyTypographyLayout();

            if (transitionFade && _va != null)
            {
                _lastTransitioning = _va.IsTransitioning;
                SetFadeAlpha01(_lastTransitioning ? 0f : 1f);
                _fadePhase = FadePhase.None;
                _pendingFadeInAfterOut = false;
            }
            else
            {
                SetFadeAlpha01(1f);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            if (_mode != null && _mode.Mode != VisualMode.VisualAssist)
                return;

            if (_rootRt == null || centerText == null)
                return;

            var s = _rootRt.rect.size;
            if (s.x <= 1f || s.y <= 1f)
                return;

            if (_lastRootSize != Vector2.zero && Vector2.SqrMagnitude(s - _lastRootSize) < 0.25f)
                return;

            _lastRootSize = s;

            ApplyTypographyLayout();

            _marquee?.Refresh(forceRestart: true);
        }

        private void Update()
        {
            if (_mode != null && _mode.Mode != _lastMode)
            {
                _lastMode = _mode.Mode;
                ApplyMode(_lastMode);
            }

            SyncFromSettings(force: false);

            if (_va == null)
                return;

            ApplyRootVisibility();

            if (dimmerPanel != null)
            {
                float dimmerStrength01 = GetDimmerStrength01Safe();
                float vaA = Mathf.Clamp01(_va.DimAlpha01);

                float target;
                if (vaA <= 0.0001f)
                {
                    target = 0f;
                }
                else if (dimmerStrength01 >= 0.999f)
                {
                    target = 1f;
                }
                else
                {
                    target = vaA * dimmerStrength01;
                }

                if (smoothDimmer)
                {
                    _currentDim = Mathf.MoveTowards(
                        _currentDim,
                        target,
                        dimmerLerpSpeed * Time.unscaledDeltaTime);
                }
                else
                {
                    _currentDim = target;
                }

                SetDimmerAlpha(_currentDim);
            }

            if (transitionFade)
            {
                bool transitioning = _va.IsTransitioning;
                if (transitioning != _lastTransitioning)
                {
                    _lastTransitioning = transitioning;
                    OnTransitioningChanged(transitioning);
                }
            }
            else
            {
                if (_fadeCo != null)
                {
                    StopCoroutine(_fadeCo);
                    _fadeCo = null;
                }
                _fadePhase = FadePhase.None;
                _pendingFadeInAfterOut = false;
                SetFadeAlpha01(1f);
            }
        }

        private void ApplyRootVisibility()
        {
            if (root == null)
                return;

            bool modeVisible = _mode != null && _mode.Mode == VisualMode.VisualAssist;
            bool wantRoot = modeVisible && (_va == null || _va.IsRootVisible);

            bool wasActive = root.activeSelf;

            if (wasActive == wantRoot)
                return;

            root.SetActive(wantRoot);

            if (!wasActive && wantRoot)
            {
                if (_fadeCo != null)
                {
                    StopCoroutine(_fadeCo);
                    _fadeCo = null;
                }

                _fadePhase = FadePhase.None;
                _pendingFadeInAfterOut = false;

                SetFadeAlpha01(0f);
                StartFadeTo(targetAlpha01: 1f, durationSeconds: Mathf.Max(0.001f, fadeInSeconds), phase: FadePhase.In);
            }
            else
            {
                if (_fadeCo != null)
                {
                    StopCoroutine(_fadeCo);
                    _fadeCo = null;
                }

                _fadePhase = FadePhase.None;
                _pendingFadeInAfterOut = false;
            }
        }

        private void RenderNow()
        {
            if (_va == null) return;

            ApplyRootVisibility();

            if (headerText != null)
                headerText.text = _va.Header ?? "";

            _latestSubFromVa = _va.SubHeader ?? "";

            if (string.IsNullOrEmpty(_lastStableSubText))
                _lastStableSubText = _latestSubFromVa;

            if (_swipeCo != null)
            {
            }
            else
            {
                if (enableSubHeaderSwipe && _subRt != null)
                {
                    int pulse = _va.ListMovePulse;
                    if (pulse != _lastPulse)
                    {
                        _lastPulse = pulse;

                        var dir = _va.LastListMoveDirection;
                        if (dir == VaListMoveDirection.None)
                        {
                            SetSubHeaderImmediate(_latestSubFromVa);
                        }
                        else
                        {
                            var oldText = (subHeaderText != null ? (subHeaderText.text ?? "") : _lastStableSubText);
                            StartSubHeaderSwipe(dir, oldText, _latestSubFromVa);
                        }
                    }
                    else
                    {
                        SetSubHeaderImmediate(_latestSubFromVa);
                    }
                }
                else
                {
                    SetSubHeaderImmediate(_latestSubFromVa);
                }
            }

            if (centerText != null)
            {
                string newCenter = _va.CenterText ?? "";
                if (!string.Equals(centerText.text, newCenter, System.StringComparison.Ordinal))
                {
                    ReleaseMarqueeGate();

                    centerText.text = newCenter;
                    ApplyTypographyLayout();
                }
            }

            _marquee?.Refresh(forceRestart: false);
        }

        private void ApplyMode(VisualMode mode)
        {
            bool visible = mode == VisualMode.VisualAssist;

            if (!visible)
            {
                _currentDim = 0f;
                _lastStableSubText = "";
                _latestSubFromVa = "";
                _lastPulse = 0;

                _lastSwipeDir = VaListMoveDirection.None;
                _lastSwipeStartTime = -999f;

                if (_swipeCo != null)
                {
                    StopCoroutine(_swipeCo);
                    _swipeCo = null;
                }

                if (_fadeCo != null)
                {
                    StopCoroutine(_fadeCo);
                    _fadeCo = null;
                }
                _fadePhase = FadePhase.None;
                _pendingFadeInAfterOut = false;
                SetFadeAlpha01(1f);

                ResetSubHeaderX();

                _lastRootSize = Vector2.zero;

                ReleaseMarqueeGate();
            }
            else
            {
                _lastRootSize = Vector2.zero;

                SyncFromSettings(force: true);
                ApplyAllTextColor();

                ApplyTypographyLayout();
                _marquee?.Refresh(forceRestart: true);
            }

            ApplyRootVisibility();

            if (dimmerPanel != null)
                SetDimmerAlpha(_currentDim);
        }

        private void SetDimmerAlpha(float a01)
        {
            if (dimmerPanel == null) return;

            var c = dimmerPanel.color;
            c.a = Mathf.Clamp01(a01);
            dimmerPanel.color = c;
        }

        private void SetSubHeaderImmediate(string text)
        {
            if (subHeaderText != null)
                subHeaderText.text = text ?? "";

            _lastStableSubText = text ?? "";
            ResetSubHeaderX();
        }

        private void ResetSubHeaderX()
        {
            if (_subRt == null) return;

            var p = _subRt.anchoredPosition;
            p.x = 0f;
            _subRt.anchoredPosition = p;
        }

        private void StartSubHeaderSwipe(VaListMoveDirection dir, string oldText, string newText)
        {
            if (_subRt == null || subHeaderText == null)
            {
                SetSubHeaderImmediate(newText);
                return;
            }

            if (dir == VaListMoveDirection.None)
            {
                SetSubHeaderImmediate(newText);
                return;
            }

            float now = Time.unscaledTime;

            bool hasActiveSwipe = _swipeCo != null;
            bool isOppositeFlip =
                hasActiveSwipe &&
                _lastSwipeDir != VaListMoveDirection.None &&
                dir != _lastSwipeDir &&
                (now - _lastSwipeStartTime) <= Mathf.Max(0.01f, oppositePulseWindowSeconds);

            if (isOppositeFlip)
            {
                ResetSubHeaderX();
            }

            _lastSwipeDir = dir;
            _lastSwipeStartTime = now;

            if (_swipeCo != null)
            {
                StopCoroutine(_swipeCo);
                _swipeCo = null;
            }

            _swipeCo = StartCoroutine(SubHeaderSwipeRoutine(dir, oldText ?? "", newText ?? ""));
        }

        private IEnumerator SubHeaderSwipeRoutine(VaListMoveDirection dir, string oldText, string initialNewText)
        {
            subHeaderText.text = oldText;

            float off = ComputeOffscreenX();
            float outTarget = (dir == VaListMoveDirection.Next) ? -off : off;

            float fromX = (_subRt != null) ? _subRt.anchoredPosition.x : 0f;
            float outDuration = ComputeDurationSeconds(fromX, outTarget);
            yield return MoveSubHeaderX(fromX, outTarget, outDuration);

            string newText = string.IsNullOrEmpty(_latestSubFromVa) ? (initialNewText ?? "") : _latestSubFromVa;

            subHeaderText.text = newText;

            float inStart = (dir == VaListMoveDirection.Next) ? off : -off;
            SetSubHeaderX(inStart);

            float inDuration = ComputeDurationSeconds(inStart, 0f);
            yield return MoveSubHeaderX(inStart, 0f, inDuration);

            SetSubHeaderX(0f);
            _lastStableSubText = newText;

            _swipeCo = null;

            if (!string.Equals(_lastStableSubText, _latestSubFromVa, System.StringComparison.Ordinal))
                SetSubHeaderImmediate(_latestSubFromVa);
        }

        private float ComputeOffscreenX()
        {
            float parentW = _subParentRt != null ? _subParentRt.rect.width : Screen.width;

            float textW = 0f;
            if (subHeaderText != null)
            {
                var v = subHeaderText.GetPreferredValues(subHeaderText.text, Mathf.Infinity, Mathf.Infinity);
                textW = Mathf.Max(0f, v.x);
            }

            textW *= Mathf.Clamp01(offscreenTextWidthFactor);

            return (parentW * 0.5f) + (textW * 0.5f) + swipeExtraGapPx;
        }

        private float ComputeDurationSeconds(float fromX, float toX)
        {
            float dist = Mathf.Abs(toX - fromX);
            if (dist <= 0.001f)
                return swipeMinDurationSeconds;

            float speed = Mathf.Max(600f, swipeSpeedPxPerSecond);
            float t = dist / speed;

            if (t < swipeMinDurationSeconds) t = swipeMinDurationSeconds;
            if (t > swipeMaxDurationSeconds) t = swipeMaxDurationSeconds;

            return t;
        }

        private IEnumerator MoveSubHeaderX(float from, float to, float seconds)
        {
            seconds = Mathf.Max(0.01f, seconds);

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / seconds);

                float eased = a * a * (3f - 2f * a);

                float x = Mathf.Lerp(from, to, eased);
                SetSubHeaderX(x);

                yield return null;
            }

            SetSubHeaderX(to);
        }

        private void SetSubHeaderX(float x)
        {
            if (_subRt == null) return;

            var p = _subRt.anchoredPosition;
            p.x = x;
            _subRt.anchoredPosition = p;
        }

        private void CaptureBaseTextColors()
        {
            if (_baseColorsCaptured)
                return;

            _headerBaseColor = headerText != null ? headerText.color : Color.white;
            _subHeaderBaseColor = subHeaderText != null ? subHeaderText.color : Color.white;
            _baseColorsCaptured = true;
        }

        private void ApplyAllTextColor()
        {
            if (!overrideAllTextColors)
                return;

            CaptureBaseTextColors();

            if (headerText != null)
            {
                headerText.color = new Color(
                    overlayFontColor.r, overlayFontColor.g, overlayFontColor.b,
                    headerText.color.a);
            }

            if (subHeaderText != null)
            {
                subHeaderText.color = new Color(
                    overlayFontColor.r, overlayFontColor.g, overlayFontColor.b,
                    subHeaderText.color.a);
            }

            if (centerText != null)
            {
                centerText.color = new Color(
                    overlayFontColor.r, overlayFontColor.g, overlayFontColor.b,
                    centerText.color.a);
            }

            _headerBaseColor = new Color(overlayFontColor.r, overlayFontColor.g, overlayFontColor.b, _headerBaseColor.a);
            _subHeaderBaseColor = new Color(overlayFontColor.r, overlayFontColor.g, overlayFontColor.b, _subHeaderBaseColor.a);
            _baseColorsCaptured = true;
        }

        private void SetFadeAlpha01(float a01)
        {
            CaptureBaseTextColors();

            _fadeAlpha01 = Mathf.Clamp01(a01);

            if (headerText != null)
            {
                var c = _headerBaseColor;
                c.a = _headerBaseColor.a * _fadeAlpha01;
                headerText.color = c;
            }

            if (subHeaderText != null)
            {
                var c = _subHeaderBaseColor;
                c.a = _subHeaderBaseColor.a * _fadeAlpha01;
                subHeaderText.color = c;
            }
        }

        private void OnTransitioningChanged(bool transitioning)
        {
            if (transitioning)
                ReleaseMarqueeGate();

            if (!transitionFade)
            {
                SetFadeAlpha01(1f);
                return;
            }

            if (root != null && !root.activeInHierarchy)
                return;

            if (transitioning)
            {
                _pendingFadeInAfterOut = false;
                StartFadeOut();
            }
            else
            {
                if (_fadePhase == FadePhase.Out && _fadeCo != null)
                {
                    _pendingFadeInAfterOut = true;
                    return;
                }

                StartFadeIn();
            }
        }

        private void StartFadeOut()
        {
            float duration = Mathf.Max(0.001f, fadeOutSeconds);
            StartFadeTo(targetAlpha01: 0f, durationSeconds: duration, phase: FadePhase.Out);
        }

        private void StartFadeIn()
        {
            float duration = Mathf.Max(0.001f, fadeInSeconds);
            StartFadeTo(targetAlpha01: 1f, durationSeconds: duration, phase: FadePhase.In);
        }

        private void StartFadeTo(float targetAlpha01, float durationSeconds, FadePhase phase)
        {
            if (_fadeCo != null)
            {
                StopCoroutine(_fadeCo);
                _fadeCo = null;
            }

            _fadePhase = phase;
            float from = _fadeAlpha01;
            float to = Mathf.Clamp01(targetAlpha01);

            if (Mathf.Abs(to - from) <= 0.001f)
            {
                SetFadeAlpha01(to);
                OnFadeFinished();
                return;
            }

            _fadeCo = StartCoroutine(FadeRoutine(from, to, durationSeconds));
        }

        private IEnumerator FadeRoutine(float from, float to, float seconds)
        {
            seconds = Mathf.Max(0.001f, seconds);

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / seconds);

                float eased = a * a * (3f - 2f * a);

                float v = Mathf.Lerp(from, to, eased);
                SetFadeAlpha01(v);

                yield return null;
            }

            SetFadeAlpha01(to);

            _fadeCo = null;
            OnFadeFinished();
        }

        private void OnFadeFinished()
        {
            if (_fadePhase == FadePhase.Out)
            {
                _fadePhase = FadePhase.None;

                if (_pendingFadeInAfterOut)
                {
                    _pendingFadeInAfterOut = false;

                    if (_va != null && !_va.IsTransitioning)
                        StartFadeIn();
                }

                return;
            }

            _fadePhase = FadePhase.None;
            _pendingFadeInAfterOut = false;
        }

        private void ApplyTypographyLayout()
        {
            if (_rootRt == null || headerText == null || subHeaderText == null || centerText == null)
                return;

            float screenH = _rootRt.rect.height;

            float topMaxH = screenH * _typo.topMaxHeightPercent;
            float bottomH = screenH * _typo.bottomReservedPercent;
            float centerAvailableH = screenH - topMaxH - bottomH;

            headerText.fontSize = _typo.headerFontSize;
            subHeaderText.fontSize = Mathf.RoundToInt(_typo.headerFontSize / _typo.headerToSubHeaderRatio);

            FitCenterText(centerAvailableH);
        }

        private void FitCenterText(float maxHeight)
        {
            if (centerText == null)
                return;

            if (_va != null && _va.IsTransitioning)
            {
                ReleaseMarqueeGate();
                return;
            }

            string text = centerText.text ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                ReleaseMarqueeGate();
                return;
            }

            RectTransform rt = centerText.rectTransform;
            float width = rt.rect.width;
            if (width <= 1f)
            {
                ReleaseMarqueeGate();
                return;
            }

            bool containsNewline = text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0;

            int chosenSize = _typo.centerFontSizes[_typo.centerFontSizes.Length - 1];

            centerText.textWrappingMode = TextWrappingModes.Normal;

            foreach (int size in _typo.centerFontSizes)
            {
                centerText.fontSize = size;

                Vector2 v = centerText.GetPreferredValues(text, width, Mathf.Infinity);

                if (v.y <= maxHeight)
                {
                    chosenSize = size;
                    break;
                }
            }

            centerText.fontSize = chosenSize;

            bool marqueeCandidate = false;

            if (!containsNewline)
            {
                centerText.textWrappingMode = TextWrappingModes.NoWrap;
                Vector2 singleLine = centerText.GetPreferredValues(text, Mathf.Infinity, Mathf.Infinity);
                marqueeCandidate = singleLine.x > width;
            }

            if (!marqueeCandidate)
            {
                centerText.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                if (_typo.marqueeFontSizeBoost > 0)
                {
                    float avgCharW = centerText.fontSize * 0.6f;
                    float visibleChars = width / avgCharW;

                    if (visibleChars >= _typo.marqueeMinVisibleChars)
                        centerText.fontSize = chosenSize + _typo.marqueeFontSizeBoost;
                }

                centerText.textWrappingMode = TextWrappingModes.NoWrap;
            }

            if (_marquee != null)
                _marquee.Refresh(forceRestart: true);

            bool isReallyMarquee = _marquee != null ? _marquee.IsActive : false;

            UpdateMarqueeGate(isReallyMarquee, text);
        }

        private void UpdateMarqueeGate(bool marqueeCandidate, string currentText)
        {
            if (_marqueeGate == null)
                return;

            if (_mode != null && _mode.Mode != VisualMode.VisualAssist)
            {
                ReleaseMarqueeGate();
                return;
            }

            if (_va != null && _va.IsTransitioning)
            {
                ReleaseMarqueeGate();
                return;
            }

            if (!marqueeCandidate)
            {
                ReleaseMarqueeGate();
                return;
            }

            if (string.Equals(_armedGateText, currentText ?? "", System.StringComparison.Ordinal))
                return;

            _armedGateText = currentText ?? "";
            _marqueeGate.BeginWaitForFirstMarqueePass();
        }

        private void OnMarqueeFirstPassCompleted()
        {
            if (_marqueeGate == null)
                return;

            if (!_marqueeGate.IsWaitingForFirstMarqueePass)
                return;

            if (_mode != null && _mode.Mode != VisualMode.VisualAssist)
            {
                ReleaseMarqueeGate();
                return;
            }

            if (_va != null && _va.IsTransitioning)
            {
                ReleaseMarqueeGate();
                return;
            }

            string current = centerText != null ? (centerText.text ?? "") : "";

            if (!string.IsNullOrEmpty(_armedGateText) &&
                !string.Equals(_armedGateText, current, System.StringComparison.Ordinal))
            {
                return;
            }

            _marqueeGate.CompleteWaitForFirstMarqueePass();
            _armedGateText = "";
        }

        private void ReleaseMarqueeGate()
        {
            _armedGateText = "";

            if (_marqueeGate == null)
                return;

            _marqueeGate.ForceRelease();
        }

        private void SyncFromSettings(bool force)
        {
            if (_settings == null || _settings.Current == null)
                return;

            var preset = _settings.Current.vaTextSizePreset;
            if (force || preset != _appliedPreset)
            {
                _appliedPreset = preset;
                _typo = VisualAssistTypographyPresets.Get(_appliedPreset);

                textSizePreset = _appliedPreset;

                ApplyTypographyLayout();
                _marquee?.Refresh(forceRestart: true);
            }

            float dimmerStrength01 = Mathf.Clamp01(_settings.Current.vaDimmerStrength01);
            if (force || !Mathf.Approximately(dimmerStrength01, _appliedDimmerStrength01))
            {
                _appliedDimmerStrength01 = dimmerStrength01;
            }

            float speedScale = Mathf.Clamp(_settings.Current.vaMarqueeSpeedScale, 0.5f, 2.0f);
            if (force || !Mathf.Approximately(speedScale, _appliedMarqueeSpeedScale))
            {
                _appliedMarqueeSpeedScale = speedScale;

                if (_marquee != null)
                {
                    _marquee.UserSpeedScale = _appliedMarqueeSpeedScale;
                    _marquee.Refresh(forceRestart: true);
                }
            }
        }

        private float GetDimmerStrength01Safe()
        {
            if (_settings == null || _settings.Current == null)
                return 1f;

            return Mathf.Clamp01(_settings.Current.vaDimmerStrength01);
        }
    }
}