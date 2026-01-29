using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;

namespace Project.Hub.Sequences
{
    public static class GameSelectCurrentSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string gameName, string gameDescriptionOrNull)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "current.game", gameName);

            if (!string.IsNullOrWhiteSpace(gameDescriptionOrNull))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", gameDescriptionOrNull);
        }
    }
}