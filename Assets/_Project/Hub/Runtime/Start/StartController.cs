using System.Collections;
using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Hub.Start.Sequences;
using Project.UI.Visual;
using UnityEngine;

namespace Project.Hub.Start
{
    public sealed class StartController : MonoBehaviour
    {
        [Header("Visual Assist UI (optional)")]
        [SerializeField] private StartVisualUiController visualUi;

        private IUiAudioOrchestrator _uiAudio;
        private IAppFlowService _flow;
        private ISettingsService _settings;
        private IVisualModeService _visualMode;
        private SpeechFeedRouter _speechFeedRouter;

        private bool _isTransitioning;

        private void Awake()
        {
            var services = AppContext.Services;

            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _speechFeedRouter = services.Resolve<SpeechFeedRouter>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();
            _visualMode = services.Resolve<IVisualModeService>();
        }

        private void OnEnable()
        {
            if (_speechFeedRouter != null && visualUi != null)
                _speechFeedRouter.SetTarget(visualUi);
        }

        private void Start()
        {
            RefreshUi();
            StartCoroutine(BootSpeechRoutine());
        }

        private IEnumerator BootSpeechRoutine()
        {
            yield return null;
            yield return null;

            PlayStandardPrompt();
        }

        private void OnDestroy()
        {
            if (_speechFeedRouter != null && visualUi != null)
                _speechFeedRouter.ClearTarget(visualUi);
        }

        public void OnRepeatRequested()
        {
            if (_isTransitioning) return;
            if (_flow != null && _flow.IsTransitioning) return;

            PlayStandardPrompt();
        }

        public async void EnterHub()
        {
            if (_isTransitioning) return;
            if (_flow != null && _flow.IsTransitioning) return;

            _isTransitioning = true;

            _uiAudio.CancelCurrent();

            try
            {
                _uiAudio.PlayGated(
                    UiAudioScope.Start,
                    "nav.to_main_menu",
                    stillTransitioning: () => _flow != null && _flow.IsTransitioning,
                    delaySeconds: 0.5f,
                    priority: SpeechPriority.High
                );

                await _flow.EnterHubAsync();
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void ExitApp()
        {
            if (_isTransitioning) return;
            if (_flow != null && _flow.IsTransitioning) return;

            _isTransitioning = true;

            _uiAudio.CancelCurrent();

            var h = _uiAudio.Play(
                UiAudioScope.Start,
                ctx => ExitAppSequence.Run(ctx),
                SpeechPriority.High,
                interruptible: false
            );

            StartCoroutine(ExitAfterSpeech(h));
        }

        private IEnumerator ExitAfterSpeech(UiAudioSequenceHandle h)
        {
            yield return WaitForUiAudioOrTimeout(h, 3f);

            var task = _flow.ExitApplicationAsync();
            while (!task.IsCompleted) yield return null;

            _isTransitioning = false;
        }

        private static IEnumerator WaitForUiAudioOrTimeout(UiAudioSequenceHandle h, float timeoutSeconds)
        {
            if (h == null) yield break;

            float t = 0f;
            while (!h.IsCompleted && !h.IsCancelled && t < timeoutSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void ToggleVisualAssist()
        {
            if (_isTransitioning) return;
            if (_flow != null && _flow.IsTransitioning) return;

            _uiAudio.CancelCurrent();

            _visualMode.ToggleVisualAssist();

            _settings.SetVisualMode(_visualMode.Mode);

            bool enabled = _visualMode.Mode == VisualMode.VisualAssist;

            _uiAudio.Play(
                UiAudioScope.Start,
                ctx => ToggleVisualAssistSequence.Run(ctx, enabled),
                SpeechPriority.High,
                interruptible: true
            );

            RefreshUi();
        }

        private void PlayStandardPrompt()
        {
            bool vaEnabled = _visualMode.Mode == VisualMode.VisualAssist;
            string controlHintKey = ResolveControlHintKey(_settings.Current);

            _uiAudio.Play(
                UiAudioScope.Start,
                ctx => StartStandardPromptSequence.Run(ctx, vaEnabled, controlHintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private static string ResolveControlHintKey(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.start_screen.touch"
                : "hint.start_screen.keyboard";
        }

        private void RefreshUi()
        {
            if (visualUi == null) return;

            visualUi.ApplyMode(_visualMode.Mode);
            visualUi.SetContent(BuildUiText());
        }

        private static string BuildUiText()
        {
            return
                "START\n\n" +
                "Keyboard:\n" +
                "- Next: Right / Down Arrow\n" +
                "- Previous: Left / Up Arrow\n" +
                "- Confirm: Enter\n" +
                "- Back: Backspace / Esc\n" +
                "- Repeat: Space\n" +
                "- Toggle Visual Assist: F1 (Start screen only)\n\n" +
                "Mouse:\n" +
                "- Next / Previous: Scroll Down / Up\n" +
                "- Confirm: Left Click\n" +
                "- Back: Right Click\n\n" +
                "Touch:\n" +
                "- Next: Swipe Left / Up\n" +
                "- Previous: Swipe Right / Down\n" +
                "- Confirm: Double-tap\n" +
                "- Back: Long-press\n" +
                "- Repeat: Single tap\n" +
                "- Toggle Visual Assist: Two-finger tap (Start screen only)";
        }
    }
}