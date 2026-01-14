using System.Collections;
using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Localization;
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
        private ILocalizationService _loc;
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
            _loc = services.Resolve<ILocalizationService>();
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
            if (_isTransitioning || _flow.IsTransitioning) return;

            PlayStandardPrompt();
        }

        public async void EnterHub()
        {
            if (_isTransitioning || _flow.IsTransitioning) return;
            _isTransitioning = true;

            _uiAudio.CancelCurrent();

            try
            {
                _uiAudio.PlayGated(
                    UiAudioScope.Start,
                    "nav.to_main_menu",
                    stillTransitioning: () => _flow.IsTransitioning,
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
            if (_isTransitioning || _flow.IsTransitioning) return;
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

        private IEnumerator WaitForUiAudioOrTimeout(UiAudioSequenceHandle h, float timeoutSeconds)
        {
            float t = 0f;

            if (h == null) yield break;

            while (!h.IsCompleted && !h.IsCancelled && t < timeoutSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void ToggleVisualAssist()
        {
            if (_isTransitioning || _flow.IsTransitioning) return;

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
            string controlHintKey = ResolveControlHintKey();

            _uiAudio.Play(
                UiAudioScope.Start,
                ctx => StartStandardPromptSequence.Run(ctx, vaEnabled, controlHintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private string ResolveControlHintKey()
        {
            var scheme = _settings.Current.preferredControlScheme;
            return scheme == Project.Core.Input.ControlScheme.Touch
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
                "Keyboard / Mouse:\n" +
                "- Confirm: Enter / Space / Left Click\n" +
                "- Back/Pause: Backspace / Esc / Right Click\n" +
                "- Toggle Visual Assist: F1 (Start screen only)\n\n" +
                "Touch gestures:\n" +
                "- Confirm: Double-tap\n" +
                "- Back/Pause: Long-press\n" +
                "- Toggle Visual Assist: Two-finger tap (Start screen only)";
        }
    }
}