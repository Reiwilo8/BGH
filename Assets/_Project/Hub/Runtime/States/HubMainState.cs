using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Hub.Sequences;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.Hub.States
{
    public enum HubMainOption
    {
        GameSelect = 0,
        Settings = 1,
        Exit = 2
    }

    public sealed class HubMainState : IHubState
    {
        private readonly HubStateMachine _sm;
        private HubMainOption _current;

        private readonly IVisualAssistService _va;

        public string Name => "Hub.Main";
        public HubMainState(HubStateMachine sm)
        {
            _sm = sm;
            _va = AppContext.Services.Resolve<IVisualAssistService>();
        }

        public void Enter()
        {
            AppContext.Services.Resolve<AppSession>().SetHubTarget(HubReturnTarget.Main);

            _current = _sm.HubMainSelection;

            RefreshVa();
            PlayPrompt();
        }

        public void Exit() { }
        public void OnFocusGained() { RefreshVa(); PlayPrompt(); }
        public void OnRepeatRequested() { RefreshVa(); PlayPrompt(); }

        public void Handle(NavAction action)
        {
            switch (action)
            {
                case NavAction.Next:
                    _current = (HubMainOption)(((int)_current + 1) % 3);
                    _sm.HubMainSelection = _current;

                    _va?.PulseListMove(VaListMoveDirection.Next);

                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _current = (HubMainOption)(((int)_current - 1 + 3) % 3);
                    _sm.HubMainSelection = _current;

                    _va?.PulseListMove(VaListMoveDirection.Previous);

                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Confirm:
                    _ = ConfirmAsync();
                    break;

                case NavAction.Back:
                    _ = ReturnToStartAsync();
                    break;
            }
        }

        private void RefreshVa()
        {
            _va?.SetHeaderKey("va.screen.main_menu");
            _va?.SetSubHeaderKey("va.current", GetOptionText());
            _va?.SetIdleHintKey(ResolveControlHintKey(_sm.Settings.Current));

            ScheduleClearTransitioning();
        }

        private void ScheduleClearTransitioning()
        {
            if (_va == null) return;
            if (!_va.IsTransitioning) return;

            if (_sm.UiAudio is MonoBehaviour mb)
                mb.StartCoroutine(ClearTransitioningNextFrame());
            else
                _va.ClearTransitioning();
        }

        private IEnumerator ClearTransitioningNextFrame()
        {
            yield return null;

            _va?.ClearTransitioning();
        }

        private string GetOptionText()
        {
            var loc = AppContext.Services.Resolve<Project.Core.Localization.ILocalizationService>();
            return loc.Get(GetOptionKey(_current));
        }

        private void PlayPrompt()
        {
            string hintKey = ResolveControlHintKey(_sm.Settings.Current);
            string optionKey = GetOptionKey(_current);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => HubMainPromptSequence.Run(ctx, ctx.Localization.Get(optionKey), hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            string optionKey = GetOptionKey(_current);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => CurrentItemSequence.Run(ctx, "current.option", ctx.Localization.Get(optionKey)),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private Task ConfirmAsync()
        {
            if (_sm.Flow.IsTransitioning)
                return Task.CompletedTask;

            switch (_current)
            {
                case HubMainOption.GameSelect:
                    _va?.NotifyTransitioning();

                    _sm.UiAudio.PlayGated(
                        UiAudioScope.Hub,
                        "nav.to_game_select",
                        stillTransitioning: () => _sm.Transitions.IsTransitioning,
                        delaySeconds: 0.5f,
                        priority: SpeechPriority.High
                    );

                    _sm.Transitions.RunInstant(() =>
                    {
                        _sm.SetState(new HubGameSelectState(_sm));
                    });
                    break;

                case HubMainOption.Settings:
                    _sm.HubMainSelection = HubMainOption.Settings;

                    _va?.NotifyTransitioning();

                    _sm.UiAudio.PlayGated(
                        UiAudioScope.Hub,
                        "nav.to_main_settings",
                        stillTransitioning: () => _sm.Transitions.IsTransitioning,
                        delaySeconds: 0.5f,
                        priority: SpeechPriority.High
                    );

                    _sm.Transitions.RunInstant(() =>
                    {
                        _sm.SetState(new HubSettingsState(_sm));
                    });
                    break;

                case HubMainOption.Exit:
                    _ = QuitAsync();
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task QuitAsync()
        {
            _sm.AudioFx?.PlayUiCue(UiCueId.ExitChime);

            _sm.UiAudio.CancelCurrent();
            _va?.NotifyTransitioning();

            var h = _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => ExitAppSequence.Run(ctx),
                SpeechPriority.High,
                interruptible: false
            );

            float start = Time.realtimeSinceStartup;
            while (h != null && !h.IsCompleted && !h.IsCancelled)
            {
                if (Time.realtimeSinceStartup - start >= 4f)
                    break;

                await Task.Yield();
            }

            await _sm.Flow.ExitApplicationAsync();
        }

        private async Task ReturnToStartAsync()
        {
            if (_sm.Flow.IsTransitioning)
                return;

            _sm.UiAudio.CancelCurrent();
            _va?.NotifyTransitioning();

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "exit.to_start_screen",
                stillTransitioning: () => _sm.Flow.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High
            );

            await _sm.Flow.EnterStartAsync();
        }

        private static string GetOptionKey(HubMainOption option)
        {
            return option switch
            {
                HubMainOption.GameSelect => "menu.main.game_select",
                HubMainOption.Settings => "common.settings",
                HubMainOption.Exit => "common.exit",
                _ => "common.exit"
            };
        }

        private static string ResolveControlHintKey(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == Project.Core.Input.ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == Project.Core.Input.ControlHintMode.Touch
                ? "hint.main_menu.touch"
                : "hint.main_menu.keyboard";
        }
    }
}