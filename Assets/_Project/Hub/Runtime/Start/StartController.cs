using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.VisualAssist;
using Project.Hub.Start.Sequences;
using UnityEngine;
using System.Collections;

namespace Project.Hub.Start
{
    public sealed class StartController : MonoBehaviour
    {
        private IUiAudioOrchestrator _uiAudio;
        private IAppFlowService _flow;
        private ISettingsService _settings;
        private IVisualModeService _visualMode;
        private IVisualAssistService _va;

        private bool _isTransitioning;

        private void Awake()
        {
            var services = AppContext.Services;

            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();
            _visualMode = services.Resolve<IVisualModeService>();
            _va = services.Resolve<IVisualAssistService>();
        }

        private void Start()
        {
            RefreshVaStatic();
            StartCoroutine(BootSpeechRoutine());
        }

        private IEnumerator BootSpeechRoutine()
        {
            yield return null;
            yield return null;

            PlayStandardPrompt();
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

            _va?.NotifyTransitioning();

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

            _va?.NotifyTransitioning();

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

            RefreshVaStatic();
        }

        private void PlayStandardPrompt()
        {
            RefreshVaStatic();

            bool vaEnabled = _visualMode.Mode == VisualMode.VisualAssist;
            string controlHintKey = ResolveControlHintKey(_settings.Current);

            _va?.SetIdleHintKey(controlHintKey);

            bool suggestLandscape = ShouldSuggestLandscapeMobile();

            _uiAudio.Play(
                UiAudioScope.Start,
                ctx => StartStandardPromptSequence.Run(ctx, vaEnabled, controlHintKey, suggestLandscape),
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

        private static bool ShouldSuggestLandscapeMobile()
        {
            if (!Application.isMobilePlatform)
                return false;

            return true;
        }

        private void RefreshVaStatic()
        {
            _va?.SetHeaderKey("va.app_name");
            _va?.SetSubHeaderKey("va.screen.start");
            _va?.ClearTransitioning();
        }
    }
}