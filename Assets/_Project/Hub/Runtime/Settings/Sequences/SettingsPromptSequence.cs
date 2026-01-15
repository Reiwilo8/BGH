using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;

namespace Project.Hub.Settings.Sequences
{
    public static class SettingsPromptSequence
    {
        public static IEnumerator Browse(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string hintKey,
            string descriptionKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.main_settings");
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descriptionKey);
        }

        public static IEnumerator Current(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string descriptionKey)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descriptionKey);
        }

        public static IEnumerator Edit(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string valueText,
            string hintKey,
            string descriptionKey)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "current.value", valueText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descriptionKey);
        }

        public static IEnumerator ConfirmAction(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string hintKey,
            string descriptionKey)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "settings.action.confirm");
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, hintKey);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, descriptionKey);
        }
    }
}