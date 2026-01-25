using System;
using System.Collections.Generic;
using Project.Core.Localization;

namespace Project.Core.VisualAssist
{
    public sealed class VisualAssistService : IVisualAssistService, IVisualAssistMarqueeGate
    {
        private readonly ILocalizationService _loc;

        public event Action Changed;

        public string Header { get; private set; } = "";
        public string SubHeader { get; private set; } = "";
        public string CenterText { get; private set; } = "";
        public float DimAlpha01 { get; private set; } = 0f;

        public bool IsTransitioning => _transitionActive;

        public int ListMovePulse { get; private set; } = 0;
        public VaListMoveDirection LastListMoveDirection { get; private set; } = VaListMoveDirection.None;

        private bool _waitingForFirstMarqueePass;

        public bool IsWaitingForFirstMarqueePass => _waitingForFirstMarqueePass;

        public void ForceRelease()
        {
            if (!_waitingForFirstMarqueePass)
                return;

            _waitingForFirstMarqueePass = false;
            RaiseChanged();
        }

        public void BeginWaitForFirstMarqueePass()
        {
            if (_waitingForFirstMarqueePass)
                return;

            _waitingForFirstMarqueePass = true;
            RaiseChanged();
        }

        public void CompleteWaitForFirstMarqueePass()
        {
            if (!_waitingForFirstMarqueePass)
                return;

            _waitingForFirstMarqueePass = false;
            RaiseChanged();
        }

        private sealed class TextSlot
        {
            public bool HasValue;
            public string Text;
        }

        private readonly Dictionary<VaCenterLayer, TextSlot> _centerLayers = new();
        private readonly TextSlot _idleHint = new();

        private bool _transitionActive;
        private float _repeatFlashUntil;

        public VisualAssistService(ILocalizationService localization)
        {
            _loc = localization;

            foreach (VaCenterLayer layer in Enum.GetValues(typeof(VaCenterLayer)))
            {
                if (layer == VaCenterLayer.None) continue;
                _centerLayers[layer] = new TextSlot();
            }

            if (_loc != null)
                _loc.LanguageChanged += _ => RaiseChanged();
        }

        public void SetHeaderKey(string key, params object[] args) => SetHeaderText(SafeGet(key, args));
        public void SetHeaderText(string text)
        {
            Header = text ?? "";
            RaiseChanged();
        }

        public void SetSubHeaderKey(string key, params object[] args) => SetSubHeaderText(SafeGet(key, args));
        public void SetSubHeaderText(string text)
        {
            SubHeader = text ?? "";
            RaiseChanged();
        }

        public void SetCenterKey(VaCenterLayer layer, string key, params object[] args) =>
            SetCenterText(layer, SafeGet(key, args));

        public void SetCenterText(VaCenterLayer layer, string text)
        {
            if (layer == VaCenterLayer.None) return;

            if (layer == VaCenterLayer.Gesture || layer == VaCenterLayer.Transition)
                ForceRelease();

            var slot = _centerLayers[layer];
            slot.HasValue = !string.IsNullOrWhiteSpace(text);
            slot.Text = text ?? "";

            RecomputeCenter();
        }

        public void ClearCenter(VaCenterLayer layer)
        {
            if (layer == VaCenterLayer.None) return;

            var slot = _centerLayers[layer];
            slot.HasValue = false;
            slot.Text = "";

            if (layer == VaCenterLayer.Transition)
            {
                _transitionActive = false;
                ForceRelease();
            }

            RecomputeCenter();
        }

        public void SetIdleHintKey(string key, params object[] args) => SetIdleHintText(SafeGet(key, args));
        public void SetIdleHintText(string text)
        {
            _idleHint.HasValue = !string.IsNullOrWhiteSpace(text);
            _idleHint.Text = text ?? "";
            RaiseChanged();
        }

        public void ClearIdleHint()
        {
            _idleHint.HasValue = false;
            _idleHint.Text = "";
            RaiseChanged();
        }

        public void NotifyPlannedSpeech(string fullText)
        {
            SetCenterText(VaCenterLayer.PlannedSpeech, fullText);
        }

        public void NotifyTransitioning()
        {
            ForceRelease();

            _transitionActive = true;
            SetCenterKey(VaCenterLayer.Transition, "va.status.transitioning");
        }

        public void ClearTransitioning()
        {
            ForceRelease();

            _transitionActive = false;
            ClearCenter(VaCenterLayer.Transition);
        }

        public void PulseListMove(VaListMoveDirection direction)
        {
            if (direction == VaListMoveDirection.None)
                return;

            LastListMoveDirection = direction;
            ListMovePulse++;
            RaiseChanged();
        }

        public void SetDimAlpha01(float alpha01)
        {
            if (alpha01 < 0f) alpha01 = 0f;
            if (alpha01 > 1f) alpha01 = 1f;

            DimAlpha01 = alpha01;
            RaiseChanged();
        }

        public void ClearDimmer()
        {
            DimAlpha01 = 0f;
            RaiseChanged();
        }

        public void FlashRepeat(float seconds = 0.25f)
        {
            _repeatFlashUntil = NowSeconds() + Math.Max(0.05f, seconds);
            RaiseChanged();
        }

        public void EvaluateIdleHint(bool canShow, float idleSeconds)
        {
            if (!canShow || _transitionActive)
            {
                ClearIdleHintLayerIfActive();
                return;
            }

            if (_repeatFlashUntil > NowSeconds())
            {
                if (!HasActive(VaCenterLayer.Gesture) && !HasActive(VaCenterLayer.Transition))
                {
                    SetCenterKey(VaCenterLayer.IdleHint, "va.repeat");
                }
                return;
            }

            if (_idleHint.HasValue && !HasActive(VaCenterLayer.Gesture) && !HasActive(VaCenterLayer.Transition))
            {
                SetCenterText(VaCenterLayer.IdleHint, _idleHint.Text);
            }
            else
            {
                ClearIdleHintLayerIfActive();
            }
        }

        private void ClearIdleHintLayerIfActive()
        {
            if (HasActive(VaCenterLayer.IdleHint))
                ClearCenter(VaCenterLayer.IdleHint);
        }

        private bool HasActive(VaCenterLayer layer)
        {
            return layer != VaCenterLayer.None &&
                   _centerLayers.TryGetValue(layer, out var slot) &&
                   slot.HasValue;
        }

        private void RecomputeCenter()
        {
            bool hasGesture = HasActive(VaCenterLayer.Gesture);
            if (hasGesture)
            {
                CenterText = _centerLayers[VaCenterLayer.Gesture].Text ?? "";
                RaiseChanged();
                return;
            }

            bool hasTransition = HasActive(VaCenterLayer.Transition);
            bool hasIdle = HasActive(VaCenterLayer.IdleHint);
            bool hasPlanned = HasActive(VaCenterLayer.PlannedSpeech);

            if (hasIdle && !hasTransition)
            {
                CenterText = _centerLayers[VaCenterLayer.IdleHint].Text ?? "";
                RaiseChanged();
                return;
            }

            if (hasPlanned)
            {
                CenterText = _centerLayers[VaCenterLayer.PlannedSpeech].Text ?? "";
                RaiseChanged();
                return;
            }

            if (hasTransition)
            {
                CenterText = _centerLayers[VaCenterLayer.Transition].Text ?? "";
                RaiseChanged();
                return;
            }

            if (hasIdle)
            {
                CenterText = _centerLayers[VaCenterLayer.IdleHint].Text ?? "";
                RaiseChanged();
                return;
            }

            CenterText = "";
            RaiseChanged();
        }

        private string SafeGet(string key, params object[] args)
        {
            if (_loc == null) return key ?? "";
            if (string.IsNullOrWhiteSpace(key)) return "";

            return (args == null || args.Length == 0)
                ? _loc.Get(key)
                : _loc.Get(key, args);
        }

        private static float NowSeconds()
        {
#if UNITY_2020_1_OR_NEWER
            return UnityEngine.Time.unscaledTime;
#else
            return (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
#endif
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}