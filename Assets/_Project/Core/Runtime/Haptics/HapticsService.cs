using System.Collections;
using Project.Core.AudioFx;
using UnityEngine;

namespace Project.Core.Haptics
{
    public sealed class HapticsService : MonoBehaviour, IHapticsService
    {
        private IHapticsBackend _backend;
        private IAudioFxService _audioFx;

        private bool _enabled = true;

        private float _strengthScale01 = 1f;
        private bool _audioFallbackEnabled = true;

        private Coroutine _hardwareContinuousRoutine;

        private Coroutine _audioContinuousRoutine;
        private int _audioContinuousToken;
        private bool _audioContinuousStopRequested;

        private Coroutine _audioTimedPulseRoutine;

        private AudioFxHandle _audioLoopHandle;

        [Header("Audio Fallback (Vibration Sound)")]
        [SerializeField] private CommonGameSoundId fallbackSoundId = CommonGameSoundId.Vibration;

        [SerializeField, Range(0.0f, 1.0f)]
        private float fallbackVolume01 = 1f;

        [Header("Pitch by Level (Fallback)")]
        [SerializeField] private float lightPitch = 1.15f;
        [SerializeField] private float mediumPitch = 1.00f;
        [SerializeField] private float strongPitch = 0.90f;

        [SerializeField] private float pitchScaleMin = 0.97f;
        [SerializeField] private float pitchScaleMax = 1.03f;

        [Header("Continuous Loop Points (seconds)")]
        [SerializeField] private float assumedClipSeconds = 0.679f;

        [SerializeField] private float loopStartSeconds = 0.275f;
        [SerializeField] private float loopEndSeconds = 0.400f;

        private const float MinLoopRegionSeconds = 0.08f;

        public bool IsHardwareSupported => _backend != null && _backend.IsSupported;

        public bool Enabled => _enabled;
        public float StrengthScale01 => _strengthScale01;
        public bool AudioFallbackEnabled => _audioFallbackEnabled;

        private bool CanUseHardware => _enabled && IsHardwareSupported;

        private bool CanUseFallback => _audioFallbackEnabled
                                       && _audioFx != null
                                       && (!IsHardwareSupported || !_enabled);

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _backend = HapticsBackendFactory.Create();
        }

        public void Init(IAudioFxService audioFx)
        {
            _audioFx = audioFx;
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;

            StopHardwareLoop();

            if (IsHardwareSupported)
            {
                try { _backend.Cancel(); } catch { }
            }

            StopAudioFallback(immediate: true);
        }

        public void SetStrengthScale01(float scale01)
        {
            _strengthScale01 = Mathf.Clamp01(scale01);
        }

        public void SetAudioFallbackEnabled(bool enabled)
        {
            _audioFallbackEnabled = enabled;
            if (!_audioFallbackEnabled)
                StopAudioFallback(immediate: true);
        }

        public void Pulse(HapticLevel level, float durationSeconds = -1f)
        {
            if (_audioTimedPulseRoutine != null)
            {
                StopCoroutine(_audioTimedPulseRoutine);
                _audioTimedPulseRoutine = null;
            }

            if (CanUseHardware)
            {
                long ms = ResolvePulseMs(level, durationSeconds);
                _backend.VibrateMilliseconds(ms);
                return;
            }

            if (!CanUseFallback)
                return;

            if (durationSeconds > 0.0001f)
            {
                _audioTimedPulseRoutine = StartCoroutine(AudioTimedPulseRoutine(level, durationSeconds));
                return;
            }

            PlayFallbackOneShot(level, startSeconds: 0f, endSeconds: 0f);
        }

        public HapticsHandle StartContinuous(HapticLevel level, float onSeconds = 0.06f, float offSeconds = 0.08f)
        {
            StopAllActive();

            var h = new HapticsHandle();

            if (CanUseHardware)
            {
                (onSeconds, offSeconds) = ResolveHardwareContinuousPattern(level, onSeconds, offSeconds);

                _hardwareContinuousRoutine = StartCoroutine(HardwareContinuousRoutine(level, onSeconds, offSeconds));
                h.StopAction = () => StopAllActive();
                return h;
            }

            if (CanUseFallback)
            {
                StartAudioContinuous(level);
                h.StopAction = () => StopAudioFallback(immediate: false);
                return h;
            }

            h.StopAction = () => { };
            return h;
        }

        public void StopAll()
        {
            StopAllActive();
        }

        private void StopAllActive()
        {
            StopHardwareLoop();
            StopAudioFallback(immediate: true);

            if (IsHardwareSupported)
            {
                try { _backend.Cancel(); } catch { }
            }
        }

        private void StopHardwareLoop()
        {
            if (_hardwareContinuousRoutine != null)
            {
                StopCoroutine(_hardwareContinuousRoutine);
                _hardwareContinuousRoutine = null;
            }
        }

        private IEnumerator HardwareContinuousRoutine(HapticLevel level, float onSeconds, float offSeconds)
        {
            onSeconds = Mathf.Max(0.01f, onSeconds);
            offSeconds = Mathf.Max(0.01f, offSeconds);

            while (CanUseHardware)
            {
                long ms = ResolvePulseMs(level, durationSeconds: onSeconds);
                _backend.VibrateMilliseconds(ms);

                float t = 0f;
                while (t < onSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                t = 0f;
                while (t < offSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _hardwareContinuousRoutine = null;
        }

        private (float onSeconds, float offSeconds) ResolveHardwareContinuousPattern(HapticLevel level, float onSeconds, float offSeconds)
        {
            bool looksDefault = Mathf.Abs(onSeconds - 0.06f) < 0.0001f && Mathf.Abs(offSeconds - 0.08f) < 0.0001f;
            if (!looksDefault)
                return (onSeconds, offSeconds);

            float s = Mathf.Clamp01(_strengthScale01);
            float onMul = Mathf.Lerp(0.85f, 1.15f, s);
            float offMul = Mathf.Lerp(1.15f, 0.85f, s);

            return level switch
            {
                HapticLevel.Light => (0.020f * onMul, 0.140f * offMul),
                HapticLevel.Medium => (0.035f * onMul, 0.085f * offMul),
                HapticLevel.Strong => (0.060f * onMul, 0.040f * offMul),
                _ => (0.035f * onMul, 0.085f * offMul)
            };
        }

        private long ResolvePulseMs(HapticLevel level, float durationSeconds)
        {
            float baseMs = level switch
            {
                HapticLevel.Light => 20f,
                HapticLevel.Medium => 45f,
                HapticLevel.Strong => 80f,
                _ => 45f
            };

            if (durationSeconds > 0f)
                baseMs = durationSeconds * 1000f;

            float scale = Mathf.Lerp(0.35f, 1.0f, Mathf.Clamp01(_strengthScale01));
            float ms = baseMs * scale;

            ms = Mathf.Clamp(ms, 10f, 500f);
            return (long)ms;
        }

        private void StartAudioContinuous(HapticLevel level)
        {
            if (!CanUseFallback)
                return;

            if (_audioContinuousRoutine != null)
            {
                StopCoroutine(_audioContinuousRoutine);
                _audioContinuousRoutine = null;
            }

            if (_audioTimedPulseRoutine != null)
            {
                StopCoroutine(_audioTimedPulseRoutine);
                _audioTimedPulseRoutine = null;
            }

            StopLoopHandle();

            _audioContinuousStopRequested = false;
            _audioContinuousToken++;
            _audioContinuousRoutine = StartCoroutine(AudioContinuousRoutine(level, _audioContinuousToken));
        }

        private void StopAudioFallback(bool immediate)
        {
            if (_audioTimedPulseRoutine != null)
            {
                StopCoroutine(_audioTimedPulseRoutine);
                _audioTimedPulseRoutine = null;
            }

            if (_audioContinuousRoutine == null)
            {
                if (immediate) StopLoopHandle();
                return;
            }

            if (immediate)
            {
                _audioContinuousToken++;
                StopCoroutine(_audioContinuousRoutine);
                _audioContinuousRoutine = null;
                _audioContinuousStopRequested = false;

                StopLoopHandle();
                return;
            }

            _audioContinuousStopRequested = true;
        }

        private void StopLoopHandle()
        {
            if (_audioLoopHandle != null)
            {
                try { _audioLoopHandle.Stop(); } catch { }
                _audioLoopHandle = null;
            }
        }

        private IEnumerator AudioTimedPulseRoutine(HapticLevel level, float durationSeconds)
        {
            if (!CanUseFallback)
                yield break;

            durationSeconds = Mathf.Clamp(durationSeconds, 0.05f, 10f);

            StartAudioContinuous(level);

            float t = 0f;
            while (t < durationSeconds)
            {
                if (!CanUseFallback)
                    break;

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            StopAudioFallback(immediate: false);

            float tail = 0f;
            while (tail < 0.35f && _audioContinuousRoutine != null)
            {
                tail += Time.unscaledDeltaTime;
                yield return null;
            }

            _audioTimedPulseRoutine = null;
        }

        private IEnumerator AudioContinuousRoutine(HapticLevel level, int token)
        {
            float clipLen = Mathf.Max(0.1f, assumedClipSeconds);

            float ls = Mathf.Clamp(loopStartSeconds, 0f, clipLen);
            float le = Mathf.Clamp(loopEndSeconds, 0f, clipLen);

            if (le <= ls + MinLoopRegionSeconds)
            {
                ls = 0f;
                le = clipLen;
            }

            float pitch = ResolveFallbackPitch(level);

            if (ls > 0.001f)
            {
                PlayFallbackOneShot(level, startSeconds: 0f, endSeconds: ls, forcedPitch: pitch);
                yield return WaitSecondsUnscaled(ls);
            }

            if (!CanUseFallback || token != _audioContinuousToken)
            {
                _audioContinuousRoutine = null;
                _audioContinuousStopRequested = false;
                yield break;
            }

            _audioLoopHandle = _audioFx.PlayCommonGameSoundControlled(
                fallbackSoundId,
                new AudioFxPlayOptions
                {
                    Volume01 = Mathf.Clamp01(fallbackVolume01),
                    Pitch = pitch,
                    PanStereo = 0f,
                    Loop = true,
                    StartTimeSeconds = ls,
                    EndTimeSeconds = 0f
                }
            );

            if (_audioLoopHandle == null || _audioLoopHandle.Source == null)
            {
                float loopDur = Mathf.Max(MinLoopRegionSeconds, le - ls);

                while (CanUseFallback && token == _audioContinuousToken && !_audioContinuousStopRequested)
                {
                    PlayFallbackOneShot(level, startSeconds: ls, endSeconds: le, forcedPitch: pitch);
                    yield return WaitSecondsUnscaled(loopDur);
                }

                if (token == _audioContinuousToken && _audioContinuousStopRequested && le < clipLen - 0.001f && CanUseFallback)
                {
                    PlayFallbackOneShot(level, startSeconds: le, endSeconds: 0f, forcedPitch: pitch);
                    yield return WaitSecondsUnscaled(Mathf.Max(0f, clipLen - le));
                }

                _audioContinuousRoutine = null;
                _audioContinuousStopRequested = false;
                yield break;
            }

            var src = _audioLoopHandle.Source;

            while (CanUseFallback && token == _audioContinuousToken && !_audioContinuousStopRequested)
            {
                if (src == null || src.clip == null)
                    break;

                if (src.isPlaying && src.time >= le)
                    src.time = ls;

                yield return null;
            }

            if (token != _audioContinuousToken)
            {
                StopLoopHandle();
                _audioContinuousRoutine = null;
                _audioContinuousStopRequested = false;
                yield break;
            }

            StopLoopHandle();

            if (CanUseFallback && _audioContinuousStopRequested && le < clipLen - 0.001f)
            {
                PlayFallbackOneShot(level, startSeconds: le, endSeconds: 0f, forcedPitch: pitch);
                yield return WaitSecondsUnscaled(Mathf.Max(0f, clipLen - le));
            }

            _audioContinuousRoutine = null;
            _audioContinuousStopRequested = false;
        }

        private void PlayFallbackOneShot(HapticLevel level, float startSeconds, float endSeconds, float? forcedPitch = null)
        {
            if (!CanUseFallback)
                return;

            var opt = new AudioFxPlayOptions
            {
                Volume01 = Mathf.Clamp01(fallbackVolume01),
                Pitch = forcedPitch ?? ResolveFallbackPitch(level),
                PanStereo = 0f,
                Loop = false,
                StartTimeSeconds = Mathf.Max(0f, startSeconds),
                EndTimeSeconds = Mathf.Max(0f, endSeconds)
            };

            opt.Clamp();

            _audioFx.PlayCommonGameSound(fallbackSoundId, opt);
        }

        private float ResolveFallbackPitch(HapticLevel level)
        {
            float basePitch = level switch
            {
                HapticLevel.Light => lightPitch,
                HapticLevel.Medium => mediumPitch,
                HapticLevel.Strong => strongPitch,
                _ => mediumPitch
            };

            float scale = Mathf.Lerp(pitchScaleMin, pitchScaleMax, Mathf.Clamp01(_strengthScale01));
            float p = basePitch * scale;

            return Mathf.Clamp(p, 0.1f, 3f);
        }

        private static IEnumerator WaitSecondsUnscaled(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}