using System.Collections;
using Project.Core.Audio.Steps;

namespace Project.Core.Audio.Sequences.Common
{
    public static class NavigateToSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string key, params object[] args)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, key, args);
        }
    }
}