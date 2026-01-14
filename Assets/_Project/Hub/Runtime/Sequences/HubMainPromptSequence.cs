using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;

namespace Project.Hub.Sequences
{
    public static class HubMainPromptSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, string optionText, string hintKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.main_menu");
            yield return CurrentItemSequence.Run(ctx, "current.option", optionText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);
        }
    }
}