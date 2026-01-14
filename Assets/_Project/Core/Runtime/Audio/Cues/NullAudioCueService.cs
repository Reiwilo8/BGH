namespace Project.Core.Audio.Cues
{
    public sealed class NullAudioCueService : IAudioCueService
    {
        public void Play(UiCue cue) { }
        public void StopAll() { }
    }
}