using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;

namespace Project.Hub.Start.Sequences
{
    public static class StartStandardPromptSequence
    {
        public static IEnumerator Run(
            UiAudioContext ctx,
            bool visualAssistEnabled,
            string controlHintKey,
            bool shouldSuggestLandscapeMobile)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.start_screen");

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "hint.audio.stereo");

            if (shouldSuggestLandscapeMobile)
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, "hint.mobile.landscape");

            if (visualAssistEnabled)
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, "app.visual_mode.status_on");

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, controlHintKey);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "hint.controls_in_settings");
        }
    }
}