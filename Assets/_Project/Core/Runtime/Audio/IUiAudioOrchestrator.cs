using Project.Core.Speech;
using System;
using System.Collections;

namespace Project.Core.Audio
{
    public interface IUiAudioOrchestrator
    {
        UiAudioSequenceHandle Play(
            UiAudioScope scope,
            Func<UiAudioContext, IEnumerator> sequence,
            SpeechPriority priority,
            bool interruptible = true);

        void CancelCurrent();

        void PlayGated(
            UiAudioScope scope,
            string key,
            Func<bool> stillTransitioning,
            float delaySeconds = 0.5f,
            SpeechPriority priority = SpeechPriority.High,
            params object[] args);
    }
}