using Project.Core.Activity;
using Project.Core.Audio;
using Project.Core.Audio.Cues;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Services;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
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

        [Header("Diagnostics")]
        [SerializeField] private bool speakOnBoot = false;

        private IServiceRegistry _services;
        public IServiceRegistry Services => _services;

        private SpeechLanguageBinder _speechLanguageBinder;

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

                visualMode = VisualMode.AudioOnly,

                controlHintMode = Project.Core.Input.ControlHintMode.Auto,

                repeatIdleSeconds = 10f,

                sfxVolume = 1f,
                cuesEnabled = true
            };

            var settings = new PlayerPrefsSettingsService(defaults);
            settings.Load();
            _services.Register<ISettingsService>(settings);

            _services.Register<IAudioCueService>(new NullAudioCueService());

            var feedRouter = new SpeechFeedRouter();
            _services.Register(feedRouter);

            var speech = SpeechServiceFactory.Create(feedRouter);
            _services.Register<ISpeechService>(speech);

            var localization = new UnityLocalizationService();
            _services.Register<ILocalizationService>(localization);

            ApplyStartupLanguage(settings, localization);

            localization.SetLanguage(settings.Current.languageCode);

            _services.Register<ISpeechLocalizer>(new SpeechLocalizer(localization, speech));
            _speechLanguageBinder = new SpeechLanguageBinder(localization, speech);

            visualModeService.SetMode(settings.Current.visualMode);

            var uiAudio = FindFirstObjectByType<UiAudioOrchestrator>();
            if (uiAudio == null)
            {
                var go = new GameObject("UiAudioOrchestrator");
                go.transform.SetParent(transform, worldPositionStays: false);
                uiAudio = go.AddComponent<UiAudioOrchestrator>();
            }

            uiAudio.Init(speech, localization);
            _services.Register<IUiAudioOrchestrator>(uiAudio);

            var appFlow = new AppFlowService(
                startScene: startSceneName,
                hubScene: hubSceneName);
            _services.Register<IAppFlowService>(appFlow);

            var inactivity = new UserInactivityService();
            _services.Register<IUserInactivityService>(inactivity);

            _services.Register<IInputService>(new InputService(inactivity));
            _services.Register<IInputFocusService>(new InputFocusService());

            var repeat = new RepeatService(inactivity, speech, appFlow);
            repeat.IdleThresholdSeconds = Mathf.Clamp(settings.Current.repeatIdleSeconds, 1f, 15f);
            _services.Register<IRepeatService>(repeat);

            settings.Save();

            if (speakOnBoot)
                speech.Speak("Speech system initialized.", SpeechPriority.High);
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