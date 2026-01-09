using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;
using Project.Games.Catalog;
using Project.Games.Definitions;

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
            Announce(includeHelp: true);
        }

        public void Exit() { }

        public void OnFocusGained()
        {
            Announce(includeHelp: true);
        }

        public void Handle(NavAction action)
        {
            var games = _catalog.games;
            if (games == null || games.Length == 0)
            {
                _sm.Speech.Speak("No games available.", SpeechPriority.High);
                if (action == NavAction.Back)
                    _sm.SetState(new HubMainState(_sm));
                return;
            }

            int count = games.Length + 1;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % count;
                    Announce(includeHelp: false);
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + count) % count;
                    Announce(includeHelp: false);
                    break;

                case NavAction.Confirm:
                    if (IsBackItem(games))
                    {
                        _sm.Speech.Speak("Back.", SpeechPriority.High);
                        _sm.SetState(new HubMainState(_sm));
                    }
                    else
                    {
                        _ = ConfirmAsync(games[_index]);
                    }
                    break;

                case NavAction.Back:
                    _sm.SetState(new HubMainState(_sm));
                    break;
            }
        }

        private bool IsBackItem(GameDefinition[] games)
        {
            return _index == games.Length;
        }

        private void Announce(bool includeHelp)
        {
            var games = _catalog.games;
            if (games == null || games.Length == 0)
                return;

            bool backItem = IsBackItem(games);

            string currentText = backItem
                ? "Back"
                : (games[_index] != null ? games[_index].displayName : "Unknown");

            string help = includeHelp
                ? " Use Next or Previous to choose. Confirm to select. Back to return."
                : " Confirm to select.";

            _sm.Speech.Speak($"Game selection. Current: {currentText}.{help}", SpeechPriority.Normal);
        }

        private async System.Threading.Tasks.Task ConfirmAsync(GameDefinition game)
        {
            if (_sm.Flow.IsTransitioning)
                return;

            if (game == null || string.IsNullOrWhiteSpace(game.gameId))
            {
                _sm.Speech.Speak("Invalid game selection.", SpeechPriority.High);
                return;
            }

            _sm.Speech.Speak($"Opening {game.displayName} menu.", SpeechPriority.High);
            await _sm.Flow.EnterGameModuleAsync(game.gameId);
        }
    }
}