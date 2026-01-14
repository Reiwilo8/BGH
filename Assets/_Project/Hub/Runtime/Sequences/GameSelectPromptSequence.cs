using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;

namespace Project.Hub.Sequences
{
    public static class GameSelectPromptSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string currentText, string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.game_select");
            yield return CurrentItemSequence.Run(ctx, "current.game", currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);
        }
    }
}