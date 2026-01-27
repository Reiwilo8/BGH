using System.Collections;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Core.Audio.Sequences.Common
{
    public static class ToggleVisualAssistSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, bool enabled)
        {
            var statusKey = enabled
                ? "app.visual_mode.on"
                : "app.visual_mode.off";

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, statusKey);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "hint.visual_mode.settings");

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }
    }
}