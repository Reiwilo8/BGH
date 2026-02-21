using Project.Core.Visual.Games.Memory;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Games.Memory
{
    public sealed class MemoryVisualOverlayView : MonoBehaviour, IMemoryVisualDriver
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Card Root")]
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private CanvasGroup cardGroup;

        [Header("Card Visuals")]
        [SerializeField] private Image cardFill;
        [SerializeField] private Image cardBorder;
        [SerializeField] private TMP_Text valueText;

        [Header("Colors (Fill)")]
        [SerializeField] private Color coveredFill = new Color(0.15f, 0.70f, 0.30f, 1f);
        [SerializeField] private Color revealedFill = new Color(0.95f, 0.80f, 0.25f, 1f);
        [SerializeField] private Color matchedFill = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("Colors (Border)")]
        [SerializeField] private Color borderColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        [SerializeField] private Color activeBorderColor = new Color(1f, 0.92f, 0.35f, 1f);

        [Header("Colors (Text)")]
        [SerializeField] private Color activeTextColor = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField] private Color matchedTextColor = new Color(0.78f, 0.78f, 0.78f, 1f);

        [Header("Typography (All States)")]
        [Tooltip("Sta³y rozmiar czcionki dla wszystkich stanów (zmiana globalna w ca³ej grze). 0 = nie nadpisuj.")]
        [SerializeField] private float valueFontSizeOverride = 0f;

        [Header("Value Visibility")]
        [SerializeField] private bool hideValueOnCovered = true;
        [SerializeField] private bool hideValueOnMatched = false;

        [Header("Fade")]
        [SerializeField] private float valueFadeSeconds = 0.10f;

        [Header("Swipe (Out/In)")]
        [SerializeField] private float swipeOutSeconds = 0.10f;
        [SerializeField] private float swipeInSeconds = 0.12f;
        [SerializeField] private float swipeMarginPx = 60f;

        private Coroutine _swipeCo;
        private Coroutine _fadeCo;

        private Vector2 _basePos;

        private enum LocalState
        {
            Covered = 0,
            Revealed = 1,
            Matched = 2
        }

        private LocalState _state;

        private void Awake()
        {
            if (cardRoot != null)
                _basePos = cardRoot.anchoredPosition;

            ApplyTypographyImmediate();
            ApplyBorderImmediate(borderColor);
            ApplyStateImmediate(LocalState.Covered, value: "");
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (root == null) return;

            if (root.activeSelf != visible)
                root.SetActive(visible);

            if (!visible)
            {
                StopSwipe();
                StopFade();
                ResetMotion();
            }
        }

        public void Swipe(MemoryVisualSwipe swipe)
        {
            if (cardRoot == null) return;

            StopSwipe();
            ResetMotion();

            _swipeCo = StartCoroutine(SwipeOutInRoutine(swipe));
        }

        public void SetCovered()
        {
            ApplyState(LocalState.Covered, value: "");
        }

        public void SetRevealed(string value)
        {
            ApplyState(LocalState.Revealed, value: value ?? "");
        }

        public void SetMatched(string value)
        {
            ApplyState(LocalState.Matched, value: value ?? "");
        }

        private void ApplyState(LocalState s, string value)
        {
            ApplyTypographyImmediate();

            if (_state == s)
            {
                ApplyVisualsForState(s);

                if (s == LocalState.Revealed || s == LocalState.Covered)
                {
                    bool forceVisible = (s == LocalState.Revealed) || (!hideValueOnCovered);
                    SetValue(s == LocalState.Covered && hideValueOnCovered ? "" : value, activeTextColor, forceVisible: forceVisible);
                }
                else if (s == LocalState.Matched && !hideValueOnMatched)
                {
                    SetValue(value, matchedTextColor, forceVisible: true);
                }

                return;
            }

            _state = s;

            ApplyVisualsForState(s);

            switch (s)
            {
                case LocalState.Covered:
                    if (hideValueOnCovered)
                        SetValue("", activeTextColor, forceVisible: false);
                    else
                        SetValue(value, activeTextColor, forceVisible: true);
                    break;

                case LocalState.Revealed:
                    SetValue(value, activeTextColor, forceVisible: true);
                    break;

                case LocalState.Matched:
                    if (hideValueOnMatched)
                        SetValue("", matchedTextColor, forceVisible: false);
                    else
                        SetValue(value, matchedTextColor, forceVisible: true);
                    break;
            }
        }

        private void ApplyStateImmediate(LocalState s, string value)
        {
            _state = s;

            ApplyTypographyImmediate();
            ApplyVisualsForState(s);

            switch (s)
            {
                case LocalState.Covered:
                    SetValueImmediate(hideValueOnCovered ? "" : value, activeTextColor);
                    break;

                case LocalState.Revealed:
                    SetValueImmediate(value, activeTextColor);
                    break;

                case LocalState.Matched:
                    SetValueImmediate(hideValueOnMatched ? "" : value, matchedTextColor);
                    break;
            }
        }

        private void ApplyVisualsForState(LocalState s)
        {
            switch (s)
            {
                case LocalState.Covered:
                case LocalState.Revealed:
                    SetFill(matchedFill);
                    ApplyBorderImmediate(activeBorderColor);
                    break;

                case LocalState.Matched:
                    SetFill(matchedFill);
                    ApplyBorderImmediate(borderColor);
                    break;
            }
        }

        private void ApplyBorderImmediate(Color c)
        {
            if (cardBorder != null)
                cardBorder.color = c;
        }

        private void SetFill(Color c)
        {
            if (cardFill != null)
                cardFill.color = c;
        }

        private void ApplyTypographyImmediate()
        {
            if (valueText == null) return;

            RectTransform rt = cardRoot;
            if (rt == null) return;

            float target = rt.rect.height * 0.6f;

            if (valueText.enableAutoSizing)
            {
                valueText.fontSizeMin = target * 0.7f;
                valueText.fontSizeMax = target;
                valueText.fontSize = target;
            }
            else
            {
                valueText.fontSize = target;
            }
        }

        private void SetValue(string text, Color color, bool forceVisible)
        {
            if (valueText == null) return;

            StopFade();
            _fadeCo = StartCoroutine(ValueFadeRoutine(text, color, forceVisible));
        }

        private void SetValueImmediate(string text, Color color)
        {
            if (valueText == null) return;

            valueText.text = text ?? "";
            float a = string.IsNullOrEmpty(valueText.text) ? 0f : 1f;
            valueText.color = new Color(color.r, color.g, color.b, a);
        }

        private IEnumerator ValueFadeRoutine(string newText, Color color, bool forceVisible)
        {
            float dur = Mathf.Max(0.01f, valueFadeSeconds);

            float fromA = valueText.color.a;

            if (fromA > 0.001f)
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float a01 = Mathf.Clamp01(t / dur);
                    float eased = a01 * a01 * (3f - 2f * a01);

                    var c = valueText.color;
                    c.a = Mathf.Lerp(fromA, 0f, eased);
                    valueText.color = c;

                    yield return null;
                }
            }

            valueText.text = newText ?? "";

            float targetA = (forceVisible && !string.IsNullOrEmpty(valueText.text)) ? 1f : 0f;
            valueText.color = new Color(color.r, color.g, color.b, 0f);

            float t2 = 0f;
            while (t2 < dur)
            {
                t2 += Time.unscaledDeltaTime;
                float a01 = Mathf.Clamp01(t2 / dur);
                float eased = a01 * a01 * (3f - 2f * a01);

                var c = valueText.color;
                c.a = Mathf.Lerp(0f, targetA, eased);
                valueText.color = c;

                yield return null;
            }

            valueText.color = new Color(color.r, color.g, color.b, targetA);
            _fadeCo = null;
        }

        private IEnumerator SwipeOutInRoutine(MemoryVisualSwipe swipe)
        {
            if (cardRoot == null)
            {
                _swipeCo = null;
                yield break;
            }

            Vector2 dir = swipe switch
            {
                MemoryVisualSwipe.Left => Vector2.left,
                MemoryVisualSwipe.Right => Vector2.right,
                MemoryVisualSwipe.Up => Vector2.up,
                MemoryVisualSwipe.Down => Vector2.down,
                _ => Vector2.zero
            };

            if (dir == Vector2.zero)
            {
                _swipeCo = null;
                yield break;
            }

            float off = ResolveOffscreenDistance(dir);

            float outDur = Mathf.Max(0.02f, swipeOutSeconds);
            float inDur = Mathf.Max(0.02f, swipeInSeconds);

            Vector2 from = _basePos;
            Vector2 outPos = _basePos + dir * off;

            float t = 0f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float a01 = Mathf.Clamp01(t / outDur);
                float eased = a01 * a01 * (3f - 2f * a01);

                cardRoot.anchoredPosition = Vector2.Lerp(from, outPos, eased);
                yield return null;
            }

            Vector2 inStart = _basePos - dir * off;
            cardRoot.anchoredPosition = inStart;

            float t2 = 0f;
            while (t2 < inDur)
            {
                t2 += Time.unscaledDeltaTime;
                float a01 = Mathf.Clamp01(t2 / inDur);
                float eased = a01 * a01 * (3f - 2f * a01);

                cardRoot.anchoredPosition = Vector2.Lerp(inStart, _basePos, eased);
                yield return null;
            }

            ResetMotion();
            _swipeCo = null;
        }

        private float ResolveOffscreenDistance(Vector2 dir)
        {
            if (cardRoot == null)
                return 600f;

            float margin = Mathf.Max(0f, swipeMarginPx);

            RectTransform parent = cardRoot.parent as RectTransform;
            float parentHalf;

            if (parent != null)
            {
                Rect pr = parent.rect;
                parentHalf = Mathf.Abs(dir.x) > 0.5f ? Mathf.Abs(pr.width) * 0.5f : Mathf.Abs(pr.height) * 0.5f;
            }
            else
            {
                parentHalf = Mathf.Abs(dir.x) > 0.5f ? Screen.width * 0.5f : Screen.height * 0.5f;
            }

            Rect cr = cardRoot.rect;
            float cardHalf = Mathf.Abs(dir.x) > 0.5f ? Mathf.Abs(cr.width) * 0.5f : Mathf.Abs(cr.height) * 0.5f;

            float off = parentHalf + cardHalf + margin;
            return Mathf.Max(200f, off);
        }

        private void StopSwipe()
        {
            if (_swipeCo != null)
            {
                StopCoroutine(_swipeCo);
                _swipeCo = null;
            }
        }

        private void StopFade()
        {
            if (_fadeCo != null)
            {
                StopCoroutine(_fadeCo);
                _fadeCo = null;
            }
        }

        private void ResetMotion()
        {
            if (cardRoot != null)
                cardRoot.anchoredPosition = _basePos;

            if (cardGroup != null)
                cardGroup.alpha = 1f;
        }
    }
}