using System;
using System.Collections;
using UnityEngine;
using Project.Core.Localization;
using Project.Core.Speech;
using Project.Core.Audio.Sequences.Common;
using Project.Core.VisualAssist;
using Project.Core.AudioFx;

namespace Project.Core.Audio
{
    public sealed class UiAudioOrchestrator : MonoBehaviour, IUiAudioOrchestrator
    {
        private Coroutine _running;
        private UiAudioSequenceHandle _currentHandle;
        private SpeechPriority _currentPriority;
        private bool _currentInterruptible;

        private ISpeechService _speech;
        private ILocalizationService _localization;
        private IVisualAssistService _visualAssist;
        private IAudioFxService _audioFx;

        private Coroutine _gate;
        private PendingRequest _pending;

        private sealed class PendingRequest
        {
            public UiAudioScope Scope;
            public Func<UiAudioContext, IEnumerator> Sequence;
            public SpeechPriority Priority;
            public bool Interruptible;
            public UiAudioSequenceHandle Handle;
        }

        public void Init(
            ISpeechService speech,
            ILocalizationService localization,
            IVisualAssistService visualAssist,
            IAudioFxService audioFx)
        {
            _speech = speech;
            _localization = localization;
            _visualAssist = visualAssist;
            _audioFx = audioFx;
        }

        public UiAudioSequenceHandle Play(
            UiAudioScope scope,
            Func<UiAudioContext, IEnumerator> sequence,
            SpeechPriority priority,
            bool interruptible = true)
        {
            if (sequence == null)
                return _currentHandle;

            if (_running == null)
                return StartNew(scope, sequence, priority, interruptible);

            if (!_currentInterruptible)
            {
                QueuePending(scope, sequence, priority, interruptible);
                return _pending.Handle;
            }

            if (priority < _currentPriority)
            {
                return _currentHandle;
            }

            CancelCurrentInternal(cancelPending: false);
            return StartNew(scope, sequence, priority, interruptible);
        }

        public void PlayGated(
            UiAudioScope scope,
            string key,
            Func<bool> stillTransitioning,
            float delaySeconds = 0.5f,
            SpeechPriority priority = SpeechPriority.High,
            params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_gate != null)
            {
                StopCoroutine(_gate);
                _gate = null;
            }

            _gate = StartCoroutine(GateRoutine(scope, key, stillTransitioning, delaySeconds, priority, args));
        }

        public void CancelCurrent()
        {
            CancelCurrentInternal(cancelPending: true);
        }

        private UiAudioSequenceHandle StartNew(
            UiAudioScope scope,
            Func<UiAudioContext, IEnumerator> sequence,
            SpeechPriority priority,
            bool interruptible)
        {
            _currentPriority = priority;
            _currentInterruptible = interruptible;

            (_visualAssist as IVisualAssistMarqueeGate)?.ForceRelease();

            _currentHandle = new UiAudioSequenceHandle();
            _running = StartCoroutine(Run(sequence, _currentHandle));

            return _currentHandle;
        }

        private void QueuePending(
            UiAudioScope scope,
            Func<UiAudioContext, IEnumerator> sequence,
            SpeechPriority priority,
            bool interruptible)
        {
            if (_pending != null && _pending.Handle != null)
            {
                _pending.Handle.Cancel();
            }

            _pending = new PendingRequest
            {
                Scope = scope,
                Sequence = sequence,
                Priority = priority,
                Interruptible = interruptible,
                Handle = new UiAudioSequenceHandle()
            };
        }

        private void CancelCurrentInternal(bool cancelPending)
        {
            if (_gate != null)
            {
                StopCoroutine(_gate);
                _gate = null;
            }

            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            _currentHandle?.Cancel();
            _currentHandle = null;

            if (cancelPending)
            {
                _pending?.Handle?.Cancel();
                _pending = null;
            }

            (_visualAssist as IVisualAssistMarqueeGate)?.ForceRelease();

            _speech?.StopAll();
        }

        private IEnumerator GateRoutine(
            UiAudioScope scope,
            string key,
            Func<bool> stillTransitioning,
            float delaySeconds,
            SpeechPriority priority,
            object[] args)
        {
            float t = 0f;
            while (t < delaySeconds)
            {
                if (stillTransitioning == null || !stillTransitioning())
                {
                    _gate = null;
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (stillTransitioning != null && stillTransitioning())
            {
                Play(
                    scope,
                    ctx => NavigateToSequence.Run(ctx, key, args),
                    priority,
                    interruptible: false
                );
            }

            _gate = null;
        }

        private IEnumerator Run(Func<UiAudioContext, IEnumerator> sequence, UiAudioSequenceHandle handle)
        {
            if (_speech == null || _localization == null)
            {
                Debug.LogError("[UiAudio] Orchestrator not initialized (speech/localization is null).");
                yield break;
            }

            if (handle == null || handle.IsCancelled)
                yield break;

            var ctx = new UiAudioContext(_speech, _localization, _visualAssist, _audioFx, handle);

            yield return sequence(ctx);

            if (handle.IsCancelled)
            {
                _running = null;
                _currentHandle = null;
                yield break;
            }

            handle.MarkCompleted();

            _running = null;
            _currentHandle = null;

            if (_pending != null && _pending.Sequence != null && _pending.Handle != null && !_pending.Handle.IsCancelled)
            {
                var pending = _pending;
                _pending = null;

                _currentPriority = pending.Priority;
                _currentInterruptible = pending.Interruptible;

                _currentHandle = pending.Handle;
                _running = StartCoroutine(Run(pending.Sequence, _currentHandle));
            }
        }
    }
}