using System.Collections;
using Project.Core.Audio.Steps;

namespace Project.Core.Audio.Sequences.Common
{
    public static class ExitToSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string exitKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, exitKey);
        }
    }
}