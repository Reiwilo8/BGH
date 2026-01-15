using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Audio.Steps;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Hub.Sequences;

namespace Project.Hub.States
{
    public sealed class HubGameSelectState : IHubState
    {
        private readonly HubStateMachine _sm;
        private readonly GameCatalog _catalog;
        private int _index;

        public string Name => "Hub.GameSelect";

        public HubGameSelectState(HubStateMachine sm)
        {
            _sm = sm;
            _catalog = AppContext.Services.Resolve<GameCatalog>();
        }

        public void Enter()
        {
            AppContext.Services.Resolve<AppSession>().SetHubTarget(HubReturnTarget.GameSelect);
            _index = 0;
            PlayPrompt();
        }

        public void Exit() { }
        public void OnFocusGained() => PlayPrompt();
        public void OnRepeatRequested() => PlayPrompt();

        public void Handle(NavAction action)
        {
            var games = _catalog.games;
            if (games == null || games.Length == 0)
            {
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
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + count) % count;
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

        private void PlayPrompt()
        {
            string hintKey = ResolveControlHintKey(_sm.Settings.Current);

            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => GameSelectPromptSequence.Run(ctx, GetCurrentText(ctx, _catalog.games), hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            _sm.UiAudio.Play(
                UiAudioScope.Hub,
                ctx => CurrentItemSequence.Run(ctx, "current.game", GetCurrentText(ctx, _catalog.games)),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void BackToMain()
        {
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

            _sm.UiAudio.PlayGated(
                UiAudioScope.Hub,
                "nav.to_game_menu",
                stillTransitioning: () => _sm.Flow.IsTransitioning,
                delaySeconds: 0.5f,
                priority: SpeechPriority.High,
                game.displayName
            );

            await _sm.Flow.EnterGameModuleAsync(game.gameId);
        }

        private bool IsBackItem(GameDefinition[] games) => _index == games.Length;

        private string GetCurrentText(UiAudioContext ctx, GameDefinition[] games)
        {
            if (games == null || games.Length == 0)
                return ctx.Localization.Get("common.back");

            if (IsBackItem(games))
                return ctx.Localization.Get("common.back");

            var g = games[_index];
            return (g != null && !string.IsNullOrWhiteSpace(g.displayName))
                ? g.displayName
                : "Unknown";
        }

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