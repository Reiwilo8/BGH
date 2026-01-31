using System.Collections;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;

namespace Project.Games.Sequences
{
    public static class GameplayPromptSequence
    {
        public static IEnumerator Run(UiAudioContext ctx, IAudioFxService audioFx)
        {
            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "enter.gameplay");

            try { audioFx?.PlayCommonGameSound(CommonGameSoundId.GameStart); }
            catch { }
        }
    }
}