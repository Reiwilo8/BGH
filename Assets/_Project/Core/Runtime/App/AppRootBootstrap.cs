using Project.Core.Activity;
using Project.Core.Audio;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Services;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.Core.App
{
    [DefaultExecutionOrder(-10000)]
    public sealed class AppRootBootstrap : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string startSceneName = "StartScene";
        [SerializeField] private string hubSceneName = "HubScene";

        [Header("AudioFx")]
        [SerializeField] private AudioFxCatalog audioFxCatalog;

        [Header("Diagnostics")]
        [SerializeField] private bool speakOnBoot = false;

        private IServiceRegistry _services;
        public IServiceRegistry Services => _services;

        private SpeechLanguageBinder _speechLanguageBinder;

        private ISettingsService _settings;
        private AudioFxService _audioFx;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            AppContext.ResetCacheForDomainReload();

            _services = new ServiceRegistry();
            _services.Register(new AppSession());

            var visualModeService = new VisualModeService();
            _services.Register<IVisualModeService>(visualModeService);

            var defaults = new AppSettingsData
            {
                languageCode = "en",
                hasUserSelectedLanguage = false,

                controlHintMode = Project.Core.Input.ControlHintMode.Auto,

                cuesEnabled = true,
                cuesVolume = 1f,
                gameVolume = 1f,

                repeatIdleSeconds = 10f,

                autoRepeatEnabled = true,
                autoRepeatIdleSeconds = 20f,

                visualMode = VisualMode.AudioOnly,

                vaTextSizePreset = VisualAssistTextSizePreset.Medium,
                vaMarqueeSpeedScale = 1.0f,
                vaDimmerStrength01 = 0.7f
            };

            var settings = new PlayerPrefsSettingsService(defaults);
            settings.Load();
            _settings = settings;
            _services.Register<ISettingsService>(settings);

            var speech = SpeechServiceFactory.Create();
            _services.Register<ISpeechService>(speech);

            var localization = new UnityLocalizationService();
            _services.Register<ILocalizationService>(localization);

            ApplyStartupLanguage(settings, localization);
            localization.SetLanguage(settings.Current.languageCode);

            _services.Register<ISpeechLocalizer>(new SpeechLocalizer(localization, speech));
            _speechLanguageBinder = new SpeechLanguageBinder(localization, speech);

            visualModeService.SetMode(settings.Current.visualMode);

            var va = new VisualAssistService(localization);
            _services.Register<IVisualAssistService>(va);

            _audioFx = GetComponent<AudioFxService>();
            if (_audioFx == null)
                _audioFx = gameObject.AddComponent<AudioFxService>();

            _audioFx.SetCatalog(audioFxCatalog);

            ApplyAudioSettings(settings.Current, _audioFx);

            _services.Register<IAudioFxService>(_audioFx);

            settings.Changed -= OnSettingsChanged;
            settings.Changed += OnSettingsChanged;

            var uiAudio = GetComponent<UiAudioOrchestrator>();
            if (uiAudio == null)
                uiAudio = gameObject.AddComponent<UiAudioOrchestrator>();

            uiAudio.Init(speech, localization, va, _audioFx);
            _services.Register<IUiAudioOrchestrator>(uiAudio);

            var appFlow = new AppFlowService(
                startScene: startSceneName,
                hubScene: hubSceneName);
            _services.Register<IAppFlowService>(appFlow);

            var inactivity = new UserInactivityService();
            _services.Register<IUserInactivityService>(inactivity);

            _services.Register<IInputService>(new InputService(inactivity));
            _services.Register<IInputFocusService>(new InputFocusService());

            var repeat = new RepeatService(inactivity, speech, appFlow, settings);
            repeat.IdleThresholdSeconds = Mathf.Clamp(settings.Current.repeatIdleSeconds, 1f, 15f);
            _services.Register<IRepeatService>(repeat);

            EnsureRepeatAutoDriver(intervalSeconds: 0.5f);

            settings.Save();

            if (speakOnBoot)
                speech.Speak("Speech system initialized.", SpeechPriority.High);
        }

        private void OnSettingsChanged()
        {
            if (_settings == null || _audioFx == null) return;
            ApplyAudioSettings(_settings.Current, _audioFx);
        }

        private static void ApplyAudioSettings(AppSettingsData s, AudioFxService audioFx)
        {
            if (s == null || audioFx == null) return;

            audioFx.SetUiCuesEnabled(s.cuesEnabled);
            audioFx.SetBusVolume01(AudioFxBus.UiCues, Mathf.Clamp01(s.cuesVolume));
            audioFx.SetBusVolume01(AudioFxBus.GameSounds, Mathf.Clamp01(s.gameVolume));
        }

        private void EnsureRepeatAutoDriver(float intervalSeconds)
        {
            var driver = GetComponent<RepeatAutoDriver>();
            if (driver == null)
                driver = gameObject.AddComponent<RepeatAutoDriver>();

            driver.enabled = true;
            driver.Init(intervalSeconds);
        }

        private void Start()
        {
            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            try
            {
                var services = AppContext.Services;

                var flow = services.Resolve<IAppFlowService>();
                var uiAudio = services.Resolve<IUiAudioOrchestrator>();

                await Task.Yield();

                var audioFx = services.Resolve<IAudioFxService>();
                audioFx?.PlayUiCue(UiCueId.WelcomeChime);

                var h = uiAudio.Play(
                    UiAudioScope.Start,
                    ctx => Project.Core.Audio.Steps.UiAudioSteps.SpeakKeyAndWait(ctx, "app.welcome"),
                    SpeechPriority.High,
                    interruptible: false
                );

                await WaitForUiAudioOrTimeoutAsync(h, 4f);

                await flow.EnterStartAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static async Task WaitForUiAudioOrTimeoutAsync(UiAudioSequenceHandle h, float timeoutSeconds)
        {
            if (h == null) return;

            float start = Time.realtimeSinceStartup;
            while (!h.IsCompleted && !h.IsCancelled)
            {
                if (Time.realtimeSinceStartup - start >= timeoutSeconds)
                    return;

                await Task.Yield();
            }
        }

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;

            _speechLanguageBinder?.Dispose();
        }

        private static void ApplyStartupLanguage(
            ISettingsService settings,
            ILocalizationService localization)
        {
            if (settings == null || localization == null)
                return;

            if (settings.Current.hasUserSelectedLanguage)
            {
                if (string.IsNullOrWhiteSpace(settings.Current.languageCode))
                    settings.Current.languageCode = "en";
                return;
            }

            var sys = StartupDefaultsResolver.ResolveSystemLanguageCode();
            if (string.IsNullOrWhiteSpace(sys))
                sys = "en";

            settings.Current.languageCode = sys;

            localization.SetLanguage(sys);
            settings.Current.languageCode = localization.CurrentLanguageCode;
        }
    }
}