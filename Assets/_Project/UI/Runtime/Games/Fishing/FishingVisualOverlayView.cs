using Project.Core.Visual.Games.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Games.Fishing
{
    public sealed class FishingVisualOverlayView : MonoBehaviour, IFishingVisualDriver
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Board")]
        [SerializeField] private RectTransform boardRoot;

        [Header("Float")]
        [SerializeField] private RectTransform floatRect;
        [SerializeField] private Image floatImg;

        [Header("Tension - Gray Base Panels")]
        [SerializeField] private Image leftBase;
        [SerializeField] private Image rightBase;

        [Header("Tension - Yellow Fill Panels")]
        [SerializeField] private Image leftFill;
        [SerializeField] private Image rightFill;

        [Header("Layout")]
        [SerializeField] private bool autoLayout = true;
        [SerializeField, Range(0.05f, 1.0f)] private float boardWidthScreenPercent = 0.78f;
        [SerializeField, Range(0.05f, 1.0f)] private float boardHeightScreenPercent = 0.70f;

        [SerializeField, Range(0.01f, 0.50f)] private float tensionPanelWidthBoardPercent = 0.18f;
        [SerializeField, Range(0.05f, 1.00f)] private float tensionPanelHeightBoardPercent = 0.96f;

        [SerializeField, Range(0.01f, 0.50f)] private float floatSizeBoardMinPercent = 0.10f;
        [SerializeField, Range(0.01f, 0.50f)] private float floatSizeBoardMaxPercent = 0.16f;

        [SerializeField] private float minFloatPixels = 26f;
        [SerializeField] private float maxFloatPixels = 140f;

        [Header("Settings")]
        [SerializeField] private float panelPulseHz = 5.0f;
        [SerializeField] private float floatBitePulseHz = 7.0f;
        [SerializeField] private float floatCatchPulseHz = 3.5f;

        [SerializeField] private float smoothingTime = 0.06f;
        [SerializeField] private float smoothingMaxSpeed = 99999f;
        [SerializeField] private float smoothingSnapDistancePx = 60f;

        [SerializeField] private float biteJitterPx = 6f;
        [SerializeField] private float biteJitterResampleSeconds = 0.06f;

        [SerializeField] private float catchBrightnessMul = 1.18f;
        [SerializeField] private float biteBrightnessMul = 1.12f;

        [SerializeField] private float blinkStepSeconds = 0.075f;

        [SerializeField] private float snapOnStateChangeSeconds = 0.10f;
        [SerializeField] private float snapOnBurstEndSeconds = 0.18f;

        [Header("Tension Visual Rules")]
        [SerializeField] private float pulseMinAlpha = 0.55f;
        [SerializeField] private float pulseMaxAlpha = 1.00f;
        [SerializeField] private float basePanelAlpha = 1.00f;

        private bool _visible;
        private bool _paused;

        private FishingVisualState _lastState;
        private bool _hasLastState;

        private Vector2 _posNow;
        private Vector2 _posVel;
        private Vector2 _posTarget;

        private float _forceSnapUntilT;

        private bool _blinkActive;
        private int _blinkIndex;
        private float _blinkNextAtT;
        private bool _blinkOn;

        private float _biteJitterNextAtT;
        private Vector2 _biteJitterOffset;

        private Color _floatBaseColor;
        private bool _initialized;

        private Vector2 _lastLayoutSize;

        private float _lastSafe01;
        private bool _lastAtLastChance;

        private void Awake()
        {
            AutoWireIfNeeded();

            SetupTensionSide(leftBase, leftFill);
            SetupTensionSide(rightBase, rightFill);

            if (floatImg != null) _floatBaseColor = floatImg.color;
            _initialized = true;

            ApplyLayoutIfNeeded(force: true);
            ApplyVisibility();
            ApplyImmediateStateDefaults();

            FinalizeTensionAfterLayout();
        }

        private void OnEnable()
        {
            AutoWireIfNeeded();

            SetupTensionSide(leftBase, leftFill);
            SetupTensionSide(rightBase, rightFill);

            ApplyLayoutIfNeeded(force: true);
            ApplyVisibility();
            ApplyImmediateStateDefaults();

            FinalizeTensionAfterLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayoutIfNeeded(force: false);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyVisibility();
            if (!visible) Reset();
        }

        public void Reset()
        {
            _blinkActive = false;
            _blinkIndex = 0;
            _blinkNextAtT = 0f;
            _blinkOn = true;

            _biteJitterNextAtT = 0f;
            _biteJitterOffset = Vector2.zero;

            _posNow = Vector2.zero;
            _posVel = Vector2.zero;
            _posTarget = Vector2.zero;
            _forceSnapUntilT = Time.unscaledTime + 0.10f;

            _hasLastState = false;

            _lastSafe01 = 0f;
            _lastAtLastChance = false;

            ApplyImmediateStateDefaults();
            FinalizeTensionAfterLayout();
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            ApplyVisibility();
            if (!paused) _forceSnapUntilT = Time.unscaledTime + 0.10f;
        }

        public void Apply(in FishingVisualState state)
        {
            var prev = _lastState;
            bool hadPrev = _hasLastState;

            _lastState = state;
            _hasLastState = true;

            _visible = state.Visible;
            _paused = state.Paused;

            ApplyLayoutIfNeeded(force: false);
            ApplyVisibility();

            ApplyTension(state);

            bool wantFloat = state.FloatVisible && _visible && !_paused;
            if (floatRect != null && floatRect.gameObject.activeSelf != wantFloat)
                floatRect.gameObject.SetActive(wantFloat);

            if (!wantFloat)
                return;

            bool shouldSnap = false;
            if (hadPrev)
            {
                if (prev.Phase != state.Phase) shouldSnap = true;
                if (prev.FishAction != state.FishAction) shouldSnap = true;

                float dd = Mathf.Abs(prev.FishDistance01 - state.FishDistance01);
                float dl = Mathf.Abs(prev.FishLateral01 - state.FishLateral01);
                if (dd >= 0.06f || dl >= 0.10f) shouldSnap = true;

                bool burstEnded = (prev.FishAction == FishingVisualFishAction.Burst && state.FishAction != FishingVisualFishAction.Burst);
                if (burstEnded)
                    _forceSnapUntilT = Mathf.Max(_forceSnapUntilT, Time.unscaledTime + Mathf.Max(0.01f, snapOnBurstEndSeconds));
                else if (shouldSnap)
                    _forceSnapUntilT = Mathf.Max(_forceSnapUntilT, Time.unscaledTime + Mathf.Max(0.01f, snapOnStateChangeSeconds));
            }
            else
            {
                _forceSnapUntilT = Mathf.Max(_forceSnapUntilT, Time.unscaledTime + 0.10f);
            }

            UpdateTargetPosition(state);

            if (Time.unscaledTime <= _forceSnapUntilT)
            {
                _posNow = _posTarget;
                _posVel = Vector2.zero;
                floatRect.anchoredPosition = _posNow;
            }
        }

        public void PulseMistakeBlink()
        {
            if (!_visible) return;
            _blinkActive = true;
            _blinkIndex = 0;
            _blinkOn = true;
            _blinkNextAtT = Time.unscaledTime;
            _forceSnapUntilT = Time.unscaledTime + 0.20f;
        }

        public void PulseCatch()
        {
            if (!_visible) return;
            _forceSnapUntilT = Time.unscaledTime + 0.12f;
        }

        private void Update()
        {
            if (!_initialized) return;
            if (!_visible) return;
            if (_paused) return;
            if (root != null && !root.activeInHierarchy) return;

            ApplyLayoutIfNeeded(force: false);

            ApplyTension(_lastState);

            bool floatActive = floatRect != null && floatRect.gameObject.activeInHierarchy;
            if (!floatActive) return;

            TickBlink();
            TickBiteJitter();
            TickPosition();
            TickFloatVisuals();
        }

        private void ApplyVisibility()
        {
            if (root == null) return;
            bool want = _visible && !_paused;
            if (root.activeSelf != want)
                root.SetActive(want);
        }

        private void ApplyImmediateStateDefaults()
        {
            ApplyTensionDefaults();

            if (floatRect != null)
                floatRect.gameObject.SetActive(false);

            if (floatImg != null)
            {
                var c = _floatBaseColor;
                c.a = 1f;
                floatImg.color = c;
            }
        }

        private void ApplyTensionDefaults()
        {
            if (leftBase != null) SetImageAlpha(leftBase, basePanelAlpha);
            if (rightBase != null) SetImageAlpha(rightBase, basePanelAlpha);

            ApplyTensionSafe01(0f, atLastChance: false);
        }

        private void ApplyTension(in FishingVisualState state)
        {
            if (leftFill == null || rightFill == null) return;

            if (leftBase != null) SetImageAlpha(leftBase, basePanelAlpha);
            if (rightBase != null) SetImageAlpha(rightBase, basePanelAlpha);

            int maxTicks = Mathf.Max(1, state.TensionMaxTicks);
            int safeTicks = Mathf.Max(1, maxTicks - 1);
            int ticks = Mathf.Clamp(state.TensionTicks, 0, maxTicks);

            float safe01 = Mathf.Clamp01(ticks / (float)safeTicks);
            bool atLastChance = (ticks >= safeTicks) && (ticks < maxTicks);

            ApplyTensionSafe01(safe01, atLastChance);
        }

        private void ApplyTensionSafe01(float safe01, bool atLastChance)
        {
            if (leftFill == null || rightFill == null) return;

            safe01 = Mathf.Clamp01(safe01);

            _lastSafe01 = safe01;
            _lastAtLastChance = atLastChance;

            float alpha = 1f;
            if (atLastChance)
            {
                float t = Time.unscaledTime;
                float s = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Max(0.01f, panelPulseHz));
                alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 0.5f + 0.5f * s);
            }

            float a = (safe01 <= 0.0001f) ? 0f : Mathf.Clamp01(alpha);

            SetImageAlpha(leftFill, a);
            SetImageAlpha(rightFill, a);

            float lh = GetMaskHeight(leftBase);
            float rh = GetMaskHeight(rightBase);

            SlideFill01(leftFill.rectTransform, safe01, lh);
            SlideFill01(rightFill.rectTransform, safe01, rh);
        }

        private static float GetMaskHeight(Image baseImg)
        {
            if (baseImg == null) return 1f;
            var rt = baseImg.rectTransform;
            if (rt == null) return 1f;
            return Mathf.Max(1f, rt.rect.height);
        }

        private static void SlideFill01(RectTransform rt, float reveal01, float fullHeight)
        {
            if (rt == null) return;

            float h = Mathf.Max(1f, fullHeight);
            float r = Mathf.Clamp01(reveal01);

            float y = Mathf.Lerp(-h, 0f, r);

            var ap = rt.anchoredPosition;
            ap.x = 0f;
            ap.y = y;
            rt.anchoredPosition = ap;
        }

        private void FinalizeTensionAfterLayout()
        {
            Canvas.ForceUpdateCanvases();
            ApplyTensionSafe01(_lastSafe01, _lastAtLastChance);
        }

        private static void SetupTensionSide(Image baseImg, Image fillImg)
        {
            if (baseImg == null || fillImg == null) return;

            var baseGo = baseImg.gameObject;
            if (baseGo.GetComponent<RectMask2D>() == null)
                baseGo.AddComponent<RectMask2D>();

            if (fillImg.transform.parent != baseImg.transform)
                fillImg.transform.SetParent(baseImg.transform, worldPositionStays: false);

            fillImg.transform.SetAsLastSibling();

            fillImg.type = Image.Type.Simple;
            fillImg.preserveAspect = false;

            var rt = fillImg.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var ap = rt.anchoredPosition;
            ap.x = 0f;
            ap.y = -99999f;
            rt.anchoredPosition = ap;
        }

        private void UpdateTargetPosition(in FishingVisualState state)
        {
            if (boardRoot == null || floatRect == null) return;

            Rect r = boardRoot.rect;
            float w = Mathf.Max(1f, r.width);
            float h = Mathf.Max(1f, r.height);

            float x01 = Mathf.Clamp01((Mathf.Clamp(state.FishLateral01, -1f, 1f) + 1f) * 0.5f);
            float y01 = Mathf.Clamp01(state.FishDistance01);

            float x = Mathf.Lerp(-w * 0.5f, +w * 0.5f, x01);
            float y = Mathf.Lerp(-h * 0.5f, +h * 0.5f, y01);

            _posTarget = new Vector2(x, y);

            if (_posNow == Vector2.zero && floatRect.anchoredPosition == Vector2.zero)
            {
                _posNow = _posTarget;
                floatRect.anchoredPosition = _posNow;
            }
        }

        private void TickPosition()
        {
            if (floatRect == null || boardRoot == null) return;

            float nowT = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            bool snap = nowT <= _forceSnapUntilT;
            if (!snap)
            {
                float d = Vector2.Distance(_posNow, _posTarget);
                if (d >= Mathf.Max(0.01f, smoothingSnapDistancePx))
                    snap = true;
            }

            if (snap)
            {
                _posNow = _posTarget;
                _posVel = Vector2.zero;
            }
            else
            {
                _posNow = Vector2.SmoothDamp(
                    _posNow,
                    _posTarget,
                    ref _posVel,
                    Mathf.Max(0.001f, smoothingTime),
                    Mathf.Max(0.01f, smoothingMaxSpeed),
                    dt
                );
            }

            Vector2 final = _posNow + _biteJitterOffset;
            floatRect.anchoredPosition = final;
        }

        private void TickBlink()
        {
            if (!_blinkActive) return;

            float t = Time.unscaledTime;
            if (t < _blinkNextAtT) return;

            _blinkIndex++;

            if (_blinkIndex == 1) _blinkOn = false;
            else if (_blinkIndex == 2) _blinkOn = true;
            else if (_blinkIndex == 3) _blinkOn = false;
            else if (_blinkIndex == 4) _blinkOn = true;
            else
            {
                _blinkActive = false;
                _blinkOn = true;
                _blinkIndex = 0;
            }

            _blinkNextAtT = t + Mathf.Max(0.01f, blinkStepSeconds);

            if (floatImg != null)
                SetImageAlpha(floatImg, _blinkOn ? 1f : 0f);
        }

        private void TickBiteJitter()
        {
            if (_blinkActive)
            {
                _biteJitterOffset = Vector2.zero;
                return;
            }

            if (!_lastState.BiteIsOn)
            {
                _biteJitterOffset = Vector2.zero;
                return;
            }

            float t = Time.unscaledTime;
            if (t < _biteJitterNextAtT) return;

            _biteJitterNextAtT = t + Mathf.Max(0.01f, biteJitterResampleSeconds);

            float j = Mathf.Max(0f, biteJitterPx);
            float x = Random.Range(-j, +j);
            float y = Random.Range(-j, +j);

            _biteJitterOffset = new Vector2(x, y);
        }

        private void TickFloatVisuals()
        {
            if (floatImg == null) return;
            if (_blinkActive) return;

            float t = Time.unscaledTime;

            Color c = _floatBaseColor;

            float a = 1f;
            float bright = 1f;

            if (_lastState.BiteIsOn)
            {
                float s = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Max(0.01f, floatBitePulseHz));
                float p = 0.5f + 0.5f * s;
                a = Mathf.Lerp(0.55f, 1.0f, p);
                bright = Mathf.Lerp(1.0f, Mathf.Max(1.0f, biteBrightnessMul), p);
            }
            else if (_lastState.CanCatchNow)
            {
                float s = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Max(0.01f, floatCatchPulseHz));
                float p = 0.5f + 0.5f * s;
                a = Mathf.Lerp(0.80f, 1.0f, p);
                bright = Mathf.Lerp(1.0f, Mathf.Max(1.0f, catchBrightnessMul), p);
            }

            c.r = Mathf.Clamp01(c.r * bright);
            c.g = Mathf.Clamp01(c.g * bright);
            c.b = Mathf.Clamp01(c.b * bright);
            c.a = Mathf.Clamp01(a);

            floatImg.color = c;
        }

        private void ApplyLayoutIfNeeded(bool force)
        {
            if (!autoLayout) return;

            RectTransform rootRt = null;
            if (root != null) rootRt = root.transform as RectTransform;
            if (rootRt == null) rootRt = transform as RectTransform;
            if (rootRt == null) return;

            Vector2 size = rootRt.rect.size;
            if (!force && (Vector2.SqrMagnitude(size - _lastLayoutSize) <= 0.25f))
                return;

            _lastLayoutSize = size;

            if (boardRoot != null)
            {
                float w = Mathf.Max(1f, size.x) * Mathf.Clamp01(boardWidthScreenPercent);
                float h = Mathf.Max(1f, size.y) * Mathf.Clamp01(boardHeightScreenPercent);

                boardRoot.anchorMin = new Vector2(0.5f, 0.5f);
                boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
                boardRoot.pivot = new Vector2(0.5f, 0.5f);
                boardRoot.anchoredPosition = Vector2.zero;
                boardRoot.sizeDelta = new Vector2(w, h);
            }

            Canvas.ForceUpdateCanvases();

            if (boardRoot != null)
            {
                Rect br = boardRoot.rect;
                float bw = Mathf.Max(1f, br.width);
                float bh = Mathf.Max(1f, br.height);

                float panelW = Mathf.Clamp(bw * Mathf.Clamp01(tensionPanelWidthBoardPercent), 10f, bw);

                ConfigureTensionBase(leftBase, isLeft: true, panelW);
                ConfigureTensionBase(rightBase, isLeft: false, panelW);

                Canvas.ForceUpdateCanvases();

                ApplyTensionSafe01(_lastSafe01, _lastAtLastChance);

                if (floatRect != null)
                {
                    float minDim = Mathf.Max(1f, Mathf.Min(bw, bh));
                    float pxA = minDim * Mathf.Clamp01(floatSizeBoardMinPercent);
                    float pxB = minDim * Mathf.Clamp01(floatSizeBoardMaxPercent);
                    float px = Mathf.Clamp(
                        Mathf.Lerp(pxA, pxB, 0.5f),
                        Mathf.Max(1f, minFloatPixels),
                        Mathf.Max(minFloatPixels, maxFloatPixels)
                    );

                    floatRect.sizeDelta = new Vector2(px, px);
                }
            }
        }

        private static void ConfigureTensionBase(Image img, bool isLeft, float w)
        {
            if (img == null) return;

            var rt = img.rectTransform;
            if (rt == null) return;

            float ax = isLeft ? 0f : 1f;

            rt.anchorMin = new Vector2(ax, 0f);
            rt.anchorMax = new Vector2(ax, 1f);
            rt.pivot = new Vector2(ax, 0.5f);

            rt.anchoredPosition = Vector2.zero;

            rt.sizeDelta = new Vector2(w, 0f);
            rt.offsetMin = new Vector2(rt.offsetMin.x, 0f);
            rt.offsetMax = new Vector2(rt.offsetMax.x, 0f);

            rt.localScale = Vector3.one;
        }

        private static void SetImageAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        private void AutoWireIfNeeded()
        {
            if (root == null)
                root = FindChildGO(transform, "Root");

            if (boardRoot == null)
                boardRoot = FindChildRT(transform, "BoardRoot");

            if (floatRect == null)
                floatRect = FindChildRT(transform, "Float");

            if (floatImg == null && floatRect != null)
                floatImg = floatRect.GetComponent<Image>();

            if (leftBase == null)
                leftBase = FindChildImage(transform, "TensionBaseLeft");

            if (rightBase == null)
                rightBase = FindChildImage(transform, "TensionBaseRight");

            if (leftFill == null)
                leftFill = FindChildImage(transform, "TensionFillLeft");

            if (rightFill == null)
                rightFill = FindChildImage(transform, "TensionFillRight");

            if (floatImg != null && _floatBaseColor == default)
                _floatBaseColor = floatImg.color;
        }

        private static GameObject FindChildGO(Transform root, string name)
        {
            var t = FindChild(root, name);
            return t != null ? t.gameObject : null;
        }

        private static RectTransform FindChildRT(Transform root, string name)
        {
            var t = FindChild(root, name);
            return t != null ? t as RectTransform : null;
        }

        private static Image FindChildImage(Transform root, string name)
        {
            var t = FindChild(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;

            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var r = FindChild(c, name);
                if (r != null) return r;
            }

            return null;
        }
    }
}