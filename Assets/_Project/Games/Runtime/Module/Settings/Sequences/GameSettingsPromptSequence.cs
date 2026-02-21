using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Games.Module.Settings.Sequences
{
    public static class GameSettingsPromptSequence
    {
        public static IEnumerator Browse(
            UiAudioContext ctx,
            string gameName,
            string currentKey,
            string currentText,
            string hintKey,
            string descriptionKey,
            string gameLocalizationTable)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.game_settings", gameName);
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return SpeakCoreThenGameTableOrText(ctx, descriptionKey, gameLocalizationTable);

            if (!string.IsNullOrWhiteSpace(hintKey))
                yield return SpeakCoreThenGameTableOrText(ctx, hintKey, gameLocalizationTable);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }

        public static IEnumerator Current(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string descriptionKey,
            string gameLocalizationTable)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return SpeakCoreThenGameTableOrText(ctx, descriptionKey, gameLocalizationTable);
        }

        public static IEnumerator Edit(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string valueText,
            string hintKey,
            string descriptionKey,
            string gameLocalizationTable)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "current.value", valueText);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return SpeakCoreThenGameTableOrText(ctx, descriptionKey, gameLocalizationTable);

            if (!string.IsNullOrWhiteSpace(hintKey))
                yield return SpeakCoreThenGameTableOrText(ctx, hintKey, gameLocalizationTable);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }

        public static IEnumerator ConfirmAction(
            UiAudioContext ctx,
            string currentKey,
            string currentText,
            string hintKey,
            string descriptionKey,
            string gameLocalizationTable)
        {
            yield return CurrentItemSequence.Run(ctx, currentKey, currentText);

            if (!string.IsNullOrWhiteSpace(descriptionKey))
                yield return SpeakCoreThenGameTableOrText(ctx, descriptionKey, gameLocalizationTable);

            if (!string.IsNullOrWhiteSpace(hintKey))
                yield return SpeakCoreThenGameTableOrText(ctx, hintKey, gameLocalizationTable);

            UiAudioSteps.PlayUiCue(ctx, UiCueId.SequenceEnd);
        }

        private static IEnumerator SpeakCoreThenGameTableOrText(
            UiAudioContext ctx,
            string key,
            string gameLocalizationTable)
        {
            if (ctx == null || ctx.Handle == null || ctx.Handle.IsCancelled)
                yield break;

            if (string.IsNullOrWhiteSpace(key))
                yield break;

            string core = null;
            try { core = ctx.Localization?.Get(key); } catch { core = null; }

            if (!IsMissingValue(core, key))
            {
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, key);
                yield break;
            }

            string fromGame = null;
            if (!string.IsNullOrWhiteSpace(gameLocalizationTable) && ctx.Localization != null)
            {
                try { fromGame = ctx.Localization.GetFromTable(gameLocalizationTable, key); }
                catch { fromGame = null; }
            }

            if (!IsMissingValue(fromGame, key))
            {
                yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", fromGame);
                yield break;
            }

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", key);
        }

        private static bool IsMissingValue(string value, string key)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            if (string.Equals(value, key, System.StringComparison.Ordinal))
                return true;

            var v = value.Trim();

            if (v.IndexOf(key, System.StringComparison.Ordinal) < 0 &&
                v.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            bool hasNotFound =
                v.IndexOf("not found", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("missing", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("no entry", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("no translation", System.StringComparison.OrdinalIgnoreCase) >= 0;

            bool hasInCoreOrTable =
                v.IndexOf("in core", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                v.IndexOf("in table", System.StringComparison.OrdinalIgnoreCase) >= 0;

            return hasNotFound || hasInCoreOrTable;
        }
    }
}