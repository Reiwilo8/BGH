using System.Collections;
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

                yield break;
            }

            while (ctx.Speech.IsSpeaking)
            {
                if (h.IsCancelled) yield break;
                yield return null;
            }
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