using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;

namespace Project.Games.Sequences
{
    public static class GameplayPromptSequence
    {
        public static IEnumerator Run(
            UiAudioContext ctx,
            string gameName,
            string modeName,
            string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.gameplay", gameName, modeName);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);
        }
    }
}