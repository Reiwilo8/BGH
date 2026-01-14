using System.Collections;
using Project.Core.Audio.Steps;

namespace Project.Core.Audio.Sequences.Common
{
    public static class ToggleVisualAssistSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, bool enabled)
        {
            var statusKey = enabled
                ? "app.visual_assist.on"
                : "app.visual_assist.off";

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, statusKey);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "hint.visual_assist.settings");
        }
    }
}