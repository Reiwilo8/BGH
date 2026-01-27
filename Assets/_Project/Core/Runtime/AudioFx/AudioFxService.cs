using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.AudioFx
{
    public sealed class AudioFxService : MonoBehaviour, IAudioFxService
    {
        [Header("Catalog")]
        [SerializeField] private AudioFxCatalog catalog;

        [Header("Pool")]
        [SerializeField] private int uiPoolSize = 8;
        [SerializeField] private int gamePoolSize = 16;

        private readonly List<AudioSource> _uiSources = new();
        private readonly List<AudioSource> _gameSources = new();

        private float _uiBusVolume01 = 1f;
        private float _gameBusVolume01 = 1f;

        private bool _uiCuesEnabled = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            BuildPool(_uiSources, uiPoolSize, "UiCuesBus");
            BuildPool(_gameSources, gamePoolSize, "GameSoundsBus");
        }

        public void SetCatalog(AudioFxCatalog newCatalog)
        {
            catalog = newCatalog;
        }

        public void PlayUiCue(UiCueId id, AudioFxPlayOptions? options = null)
        {
            _ = PlayUiCueInternal(id, controlled: false, options);
        }

        public AudioFxHandle PlayUiCueControlled(UiCueId id, AudioFxPlayOptions? options = null)
        {
            return PlayUiCueInternal(id, controlled: true, options);
        }

        private AudioFxHandle PlayUiCueInternal(UiCueId id, bool controlled, AudioFxPlayOptions? options)
        {
            if (!_uiCuesEnabled) return null;
            if (catalog == null) return null;

            if (!catalog.TryGet(id, out var clip, out var defaultVol))
                return null;

            var src = AcquireFree(_uiSources);
            if (src == null) return null;

            AudioFxPlayOptions opt = options ?? ApplyUiCueDefaults(id, AudioFxPlayOptions.Default);
            opt.Clamp();

            float vol = Mathf.Clamp01(defaultVol * opt.Volume01 * _uiBusVolume01);

            ConfigureSource(src, clip, vol, opt);

            TrySeekAndRegion(src, opt);

            src.Play();

            if (!controlled) return null;

            return new AudioFxHandle { Source = src };
        }

        public AudioFxHandle PlayGameClip(AudioClip clip, AudioFxPlayOptions? options = null)
        {
            if (clip == null) return null;

            var src = AcquireFree(_gameSources);
            if (src == null) return null;

            var opt = options ?? AudioFxPlayOptions.Default;
            opt.Clamp();

            float vol = Mathf.Clamp01(opt.Volume01 * _gameBusVolume01);

            ConfigureSource(src, clip, vol, opt);
            TrySeekAndRegion(src, opt);

            src.Play();

            return new AudioFxHandle { Source = src };
        }

        public void StopAll(AudioFxBus bus)
        {
            var list = bus == AudioFxBus.UiCues ? _uiSources : _gameSources;

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null) continue;
                s.Stop();
                s.clip = null;
            }
        }

        public void SetBusVolume01(AudioFxBus bus, float volume01)
        {
            volume01 = Mathf.Clamp01(volume01);

            if (bus == AudioFxBus.UiCues) _uiBusVolume01 = volume01;
            else _gameBusVolume01 = volume01;
        }

        public void SetUiCuesEnabled(bool enabled)
        {
            _uiCuesEnabled = enabled;
            if (!enabled)
                StopAll(AudioFxBus.UiCues);
        }

        private void Update()
        {
            CleanupFinished(_uiSources);
            CleanupFinished(_gameSources);
        }

        private static void CleanupFinished(List<AudioSource> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null) continue;

                if (!s.isPlaying && !s.loop && s.clip != null)
                    s.clip = null;
            }
        }

        private static void BuildPool(List<AudioSource> target, int size, string busName)
        {
            var busGo = new GameObject(busName);
            busGo.transform.SetParent(null, worldPositionStays: false);
            DontDestroyOnLoad(busGo);

            for (int i = 0; i < size; i++)
            {
                var go = new GameObject($"AudioSource_{i:00}");
                go.transform.SetParent(busGo.transform, worldPositionStays: false);

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.clip = null;

                target.Add(src);
            }
        }

        private static AudioSource AcquireFree(List<AudioSource> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null) continue;

                if (!s.isPlaying && s.clip == null)
                    return s;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if (s == null) continue;
                if (s.loop) continue;

                s.Stop();
                s.clip = null;
                return s;
            }

            return null;
        }

        private static void ConfigureSource(AudioSource src, AudioClip clip, float volume01, AudioFxPlayOptions opt)
        {
            src.clip = clip;
            src.volume = volume01;
            src.pitch = opt.Pitch;
            src.panStereo = opt.PanStereo;
            src.loop = opt.Loop;
            src.spatialBlend = 0f;
        }

        private static void TrySeekAndRegion(AudioSource src, AudioFxPlayOptions opt)
        {
            if (src.clip == null) return;

            if (opt.StartTimeSeconds > 0f)
            {
                float t = Mathf.Clamp(opt.StartTimeSeconds, 0f, src.clip.length);
                src.time = t;
            }
        }

        private static AudioFxPlayOptions ApplyUiCueDefaults(UiCueId id, AudioFxPlayOptions opt)
        {
            switch (id)
            {
                case UiCueId.NavigateNext:
                    opt.PanStereo = +0.85f;
                    opt.Pitch = 1.0f;
                    break;

                case UiCueId.NavigatePrevious:
                    opt.PanStereo = -0.85f;
                    opt.Pitch = 1.0f;
                    break;

                case UiCueId.Increase:
                    opt.PanStereo = +0.85f;
                    opt.Pitch = 1.12f;
                    break;

                case UiCueId.Decrease:
                    opt.PanStereo = -0.85f;
                    opt.Pitch = 0.92f;
                    break;
            }

            return opt;
        }
    }
}