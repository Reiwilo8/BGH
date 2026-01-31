using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Games.Sequences
{
    public static class PausePromptSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.pause");
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }
    }
}