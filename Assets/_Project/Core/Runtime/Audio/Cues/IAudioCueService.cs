namespace Project.Core.Audio.Cues
{
    public interface IAudioCueService
    {
        void Play(UiCue cue);
        void StopAll();
    }
}