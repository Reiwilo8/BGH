using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;

namespace Project.Hub.Sequences
{
    public static class GameSelectCurrentSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string gameName, string gameDescriptionOrNull)
        {
            if (ctx == null)
                yield break;

            bool isBackItem =
                string.IsNullOrWhiteSpace(gameDescriptionOrNull) &&
                string.Equals(gameName, ctx.Localization.Get("common.back"), System.StringComparison.Ordinal);

            string key = isBackItem ? "current.option" : "current.game";

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, key, gameName);

            if (!isBackItem && !string.IsNullOrWhiteSpace(gameDescriptionOrNull))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", gameDescriptionOrNull);
        }
    }
}