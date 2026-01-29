namespace Project.Core.AudioFx
{
    public interface IAudioFxService
    {
        void SetUiCuesCatalog(UiCuesCatalog catalog);

        void PlayUiCue(UiCueId id, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayUiCueControlled(UiCueId id, AudioFxPlayOptions? options = null);

        void SetCommonGameSoundsCatalog(CommonGameSoundsCatalog catalog);

        void PlayCommonGameSound(CommonGameSoundId id, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayCommonGameSoundControlled(CommonGameSoundId id, AudioFxPlayOptions? options = null);

        void SetGameCatalogRegistry(GameAudioCatalogRegistry registry);

        void PlayGameSound(string gameId, string soundId, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayGameSoundControlled(string gameId, string soundId, AudioFxPlayOptions? options = null);

        void SetCurrentGameResolver(System.Func<string> getCurrentGameId);
        void PlayCurrentGameSound(string soundId, AudioFxPlayOptions? options = null);
        AudioFxHandle PlayCurrentGameSoundControlled(string soundId, AudioFxPlayOptions? options = null);

        AudioFxHandle PlayGameClip(UnityEngine.AudioClip clip, AudioFxPlayOptions? options = null);

        void StopAll(AudioFxBus bus);
        void SetBusVolume01(AudioFxBus bus, float volume01);

        void SetUiCuesEnabled(bool enabled);
    }
}