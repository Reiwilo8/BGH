namespace Project.Core.AudioFx
{
    public interface IAudioFxService
    {
        void SetCatalog(AudioFxCatalog catalog);

        void PlayUiCue(UiCueId id, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayUiCueControlled(UiCueId id, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayGameClip(UnityEngine.AudioClip clip, AudioFxPlayOptions? options = null);

        void StopAll(AudioFxBus bus);
        void SetBusVolume01(AudioFxBus bus, float volume01);

        void SetUiCuesEnabled(bool enabled);
    }
}