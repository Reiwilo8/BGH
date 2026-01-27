using System.Collections;
using Project.Core.AudioFx;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Core.Audio.Steps
{
    public static class UiAudioSteps
    {
        public static IEnumerator SpeakKeyAndWait(
            UiAudioContext ctx,
            string key,
            params object[] args)
        {
            if (ctx == null) yield break;
            var h = ctx.Handle;
            if (h == null || h.IsCancelled) yield break;

            string text = (args == null || args.Length == 0)
                ? ctx.Localization.Get(key)
                : ctx.Localization.Get(key, args);

            var gate = ctx.VisualAssist as IVisualAssistMarqueeGate;

            ctx.VisualAssist?.NotifyPlannedSpeech(text);

            ctx.Speech.Speak(text);

            yield return null;

            const float startDetectTimeout = 0.75f;
            float t = 0f;

            while (!h.IsCancelled && !ctx.Speech.IsSpeaking && t < startDetectTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!h.IsCancelled && !ctx.Speech.IsSpeaking)
            {
                const float protectSeconds = 0.35f;
                float p = 0f;
                while (!h.IsCancelled && p < protectSeconds)
                {
                    p += Time.unscaledDeltaTime;
                    yield return null;
                }

                gate?.ForceRelease();
                yield break;
            }

            while (ctx.Speech.IsSpeaking)
            {
                if (h.IsCancelled)
                {
                    gate?.ForceRelease();
                    yield break;
                }
                yield return null;
            }

            if (gate != null && gate.IsWaitingForFirstMarqueePass)
            {
                while (!h.IsCancelled && gate.IsWaitingForFirstMarqueePass)
                {
                    yield return null;
                }

                if (h.IsCancelled)
                {
                    gate.ForceRelease();
                    yield break;
                }
            }
        }

        public static void PlayUiCue(
            UiAudioContext ctx,
            UiCueId cueId)
        {
            if (ctx == null) return;
            if (ctx.Handle == null || ctx.Handle.IsCancelled) return;

            ctx.AudioFx?.PlayUiCue(cueId);
        }

        public static IEnumerator PauseSeconds(
            UiAudioContext ctx,
            float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (ctx.Handle.IsCancelled)
                    yield break;

                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}