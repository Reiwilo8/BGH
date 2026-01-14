using System.Collections;
using Project.Core.Audio.Steps;

namespace Project.Core.Audio.Sequences.Common
{
    public static class ExitAppSequence
    {
        public static IEnumerator Run(UiAudioContext ctx)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "app.exit");
        }
    }
}