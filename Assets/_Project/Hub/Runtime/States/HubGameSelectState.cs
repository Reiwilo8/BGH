using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Games.Localization;
using Project.Hub.Sequences;
using System.Collections;
using UnityEngine;

namespace Project.Hub.States
{
    public sealed class HubGameSelectState : IHubState
    {
        private readonly HubStateMachine _sm;
        private readonly GameCatalog _catalog;
        private readonly ILocalizationService _loc;
        private readonly IVisualAssistService _va;

        private int _index;

        public string Name => "Hub.GameSelect";

        public HubGameSelectState(HubStateMachine sm)
        {
            _sm = sm;

            var services = AppContext.Services;

            _catalog = services.Resolve<GameCatalog>();
            _loc = services.Resolve<ILocalizationService>();
            _va = services.Resolve<IVisualAssistService>();
        }

        public void Enter()
        {
            AppContext.Services.Resolve<AppSession>().SetHubTarget(HubReturnTarget.GameSelect);
            _index = 0;

            RefreshVa();
            PlayPrompt();
        }

        public void Exit() { }

        public void OnFocusGained()
        {
            RefreshVa();
            PlayPrompt();
        }

        public void OnRepeatRequested()
        {
            RefreshVa();
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            var games = _catalog.games;
            if (games == null || games.Length == 0)
            {
                RefreshVa();

                _sm.UiAudio.Play(
                    UiAudioScope.Hub,
                    ctx => UiAudioSteps.SpeakKeyAndWait(ctx, "current.game", "None"),
                    SpeechPriority.High,
                    interruptible: true
                );

                if (action == NavAction.Back)
                    BackToMain();

                return;
            }

            int count = games.Length + 1;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % count;
                    _va?.PulseListMove(VaListMoveDirection.Next);
                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + count) % count;
                    _va?.PulseListMove(VaListMoveDirection.Previous);
                    RefreshVa();
                    PlayCurrent();
                    break;

                case NavAction.Confirm:
                    if (IsBackItem(games))
                        BackToMain();
                    else
                        _ = ConfirmGameAsync(games[_index]);
                    break;

                case NavAction.Back:
                    BackToMain();
                    break;
            }
        }

        public bool IsConfirmingBackItem()
        {
            var games = _catalog.games;
            if (games == null) return true;
            return IsBackItem(games);
        }

        private void RefreshVa()
        {
            _va?.SetHeaderKey("va.screen.game_select");
            _va?.SetSubHeaderText(GetCurrentTextForVa(_catalog.games));
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

        private string GetCurrentTextForVa(GameDefinition[] games)
        {
            if (games == null || games.Length == 0)
                return _loc.Get("va.current", _loc.Get("common.back"));

            if (IsBackItem(games))
                return _loc.Get("va.current", _loc.Get("common.back"));

            var g = games[_index];
            var name = GameLocalization.GetGameName(_loc, g);

            return _loc.Get("va.game", name);
        }

        private void PlayPrompt()
        {
            string hintKey = ResolveControlHintKey(_sm.Settings.Current);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx =>
                {
                    var (name, desc) = GetCurrentNameAndDesc(ctx, _catalog.games);
                    return GameSelectPromptSequence.Run(ctx, name, desc, hintKey);
                },
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx =>
                {
                    var (name, desc) = GetCurrentNameAndDesc(ctx, _catalog.games);
                    return GameSelectCurrentSequence.Run(ctx, name, desc);
                },
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private (string name, string descOrNull) GetCurrentNameAndDesc(UiAudioContext ctx, GameDefinition[] games)
        {
            if (games == null || games.Length == 0 || IsBackItem(games))
                return (ctx.Localization.Get("common.back"), null);

            var g = games[_index];

            var name = GameLocalization.GetGameName(ctx.Localization, g);

            var desc = GameLocalization.GetGameDescription(ctx.Localization, g);
            if (string.IsNullOrWhiteSpace(desc))
                desc = null;

            return (name, desc);
        }

        private void BackToMain()
        {
            _va?.NotifyTransitioning();

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "exit.to_main_menu",
                stillTransitioning: () => _sm.Transitions.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High
            );

            _sm.Transitions.RunInstant(() =>
            {
                _sm.SetState(new HubMainState(_sm));
            });
        }

        private async System.Threading.Tasks.Task ConfirmGameAsync(GameDefinition game)
        {
            if (_sm.Flow.IsTransitioning)
                return;

            if (game == null || string.IsNullOrWhiteSpace(game.gameId))
                return;

            _va?.NotifyTransitioning();

            string gameName = GameLocalization.GetGameName(_loc, game);

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "nav.to_game_menu",
                stillTransitioning: () => _sm.Flow.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High,
                gameName
            );

            await _sm.Flow.EnterGameModuleAsync(game.gameId);
        }

        private bool IsBackItem(GameDefinition[] games) => _index == games.Length;

        private static string ResolveControlHintKey(AppSettingsData settings)
        {
            var mode = settings.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.game_select.touch"
                : "hint.game_select.keyboard";
        }
    }
}