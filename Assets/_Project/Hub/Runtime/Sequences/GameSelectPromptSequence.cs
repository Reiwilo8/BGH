using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Hub.Sequences
{
    public static class GameSelectPromptSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string gameName, string gameDescriptionOrNull, string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.game_select");
            yield return GameSelectCurrentSequence.Run(ctx, gameName, gameDescriptionOrNull);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }
    }
}