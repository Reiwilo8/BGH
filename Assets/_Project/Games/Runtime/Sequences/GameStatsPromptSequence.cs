using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Games.Sequences
{
    public static class GameStatsPromptSequence
    {
        public static IEnumerator Run(
            UiAudioContext ctx,
            string gameName,
            string currentKey,
            string currentText,
            string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.game_stats", gameName);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }
    }
}