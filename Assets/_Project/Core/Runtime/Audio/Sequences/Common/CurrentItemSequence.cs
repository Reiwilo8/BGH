using System.Collections;
using Project.Core.Audio.Steps;

namespace Project.Core.Audio.Sequences.Common
{
    public static class CurrentItemSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string currentKey, string itemText)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, currentKey, itemText);
        }
    }
}