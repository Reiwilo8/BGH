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
                preferredControlScheme = StartupDefaultsResolver.ResolvePlatformPreferredControlScheme(),
                hasUserSelectedControlScheme = false
            };

            var settings = new PlayerPrefsSettingsService(defaults);
            settings.Load();

            if (!settings.Current.hasUserSelectedLanguage)
            {
                var sysLang = StartupDefaultsResolver.ResolveSystemLanguageCode();
                if (!string.IsNullOrWhiteSpace(sysLang))
                    settings.Current.languageCode = sysLang;
            }

            if (!settings.Current.hasUserSelectedControlScheme)
            {
                settings.Current.preferredControlScheme =
                    StartupDefaultsResolver.ResolvePlatformPreferredControlScheme();
            }

            settings.Save();
            _services.Register<ISettingsService>(settings);

            _services.Register<IAudioCueService>(new NullAudioCueService());

            visualModeService.SetMode(settings.Current.visualMode);

            var feedRouter = new SpeechFeedRouter();
            _services.Register(feedRouter);

            var speech = SpeechServiceFactory.Create(feedRouter);
            _services.Register<ISpeechService>(speech);

            var localization = new UnityLocalizationService();
            _services.Register<ILocalizationService>(localization);

            localization.SetLanguage(settings.Current.languageCode);

            _services.Register<ISpeechLocalizer>(
                new SpeechLocalizer(localization, speech));

            _speechLanguageBinder = new SpeechLanguageBinder(localization, speech);

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

            _services.Register<IInputService>(
                new InputService(inactivity));

            _services.Register<IInputFocusService>(
                new InputFocusService());

            var repeat = new RepeatService(inactivity, speech, appFlow)
            {
                IdleThresholdSeconds = 10f
            };
            _services.Register<IRepeatService>(repeat);

            if (speakOnBoot)
                speech.Speak(
                    "Speech system initialized.",
                    SpeechPriority.High);
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
                var loc = services.Resolve<ILocalizationService>();
                var speech = services.Resolve<ISpeechService>();

                await Task.Yield();

                var welcomeText = loc.Get("app.welcome");
                speech.Speak(welcomeText, SpeechPriority.High);

                await Task.Delay(500);

                await flow.EnterStartAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            _speechLanguageBinder?.Dispose();
        }
    }
}