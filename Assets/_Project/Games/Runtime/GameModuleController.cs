using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;
using Project.Games.Catalog;
using Project.Games.Definitions;
using UnityEngine;

namespace Project.Games.Module
{
    public sealed class GameModuleController : MonoBehaviour
    {
        private ISpeechService _speech;
        private IAppFlowService _flow;
        private AppSession _session;
        private GameCatalog _catalog;

        private GameDefinition _game;

        private int _index;
        private MenuItem[] _items;

        private enum MenuItemKind { Mode, Settings, Back }

        private sealed class MenuItem
        {
            public MenuItemKind Kind;
            public GameModeDefinition Mode;
        }

        private void Awake()
        {
            _speech = AppContext.Services.Resolve<ISpeechService>();
            _flow = AppContext.Services.Resolve<IAppFlowService>();
            _session = AppContext.Services.Resolve<AppSession>();
            _catalog = AppContext.Services.Resolve<GameCatalog>();
        }

        private void Start()
        {
            LoadSelectedGameOrFail();
            BuildMenu();
            _index = ResolveInitialIndex();
            Announce(includeHelp: true);
        }

        public void Handle(NavAction action)
        {
            if (_items == null || _items.Length == 0) return;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % _items.Length;
                    Announce(includeHelp: false);
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + _items.Length) % _items.Length;
                    Announce(includeHelp: false);
                    break;

                case NavAction.Confirm:
                    _ = ConfirmAsync();
                    break;

                case NavAction.Back:
                    _ = BackAsync();
                    break;
            }
        }

        private void LoadSelectedGameOrFail()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
            {
                _speech.Speak("No game selected. Returning to Hub.", SpeechPriority.High);
                _ = _flow.ReturnToHubAsync();
                return;
            }

            _game = _catalog.GetById(_session.SelectedGameId);
            if (_game == null)
            {
                _speech.Speak("Selected game is missing from catalog. Returning to Hub.", SpeechPriority.High);
                _ = _flow.ReturnToHubAsync();
            }
        }

        private void BuildMenu()
        {
            var modes = _game != null ? _game.modes : null;
            int modeCount = modes != null ? modes.Length : 0;

            _items = new MenuItem[modeCount + 2];

            for (int i = 0; i < modeCount; i++)
            {
                _items[i] = new MenuItem { Kind = MenuItemKind.Mode, Mode = modes[i] };
            }

            _items[modeCount + 0] = new MenuItem { Kind = MenuItemKind.Settings };
            _items[modeCount + 1] = new MenuItem { Kind = MenuItemKind.Back };
        }

        private void Announce(bool includeHelp)
        {
            if (_game == null || _items == null || _items.Length == 0) return;

            string header = $"Game menu. {_game.displayName}.";
            string current = DescribeItem(_items[_index]);

            string help = includeHelp
                ? " Use Next or Previous to choose. Confirm to select. Back to return to game selection."
                : " Confirm to select.";

            _speech.Speak($"{header} Current: {current}.{help}", SpeechPriority.Normal);
        }

        private string DescribeItem(MenuItem item)
        {
            if (item == null) return "Unknown";

            return item.Kind switch
            {
                MenuItemKind.Mode => item.Mode != null ? item.Mode.displayName : "Unknown mode",
                MenuItemKind.Settings => "Settings",
                MenuItemKind.Back => "Back",
                _ => "Unknown"
            };
        }

        private async System.Threading.Tasks.Task ConfirmAsync()
        {
            if (_flow.IsTransitioning) return;

            var item = _items[_index];
            switch (item.Kind)
            {
                case MenuItemKind.Mode:
                    if (item.Mode == null || string.IsNullOrWhiteSpace(item.Mode.modeId))
                    {
                        _speech.Speak("Invalid mode.", SpeechPriority.High);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_game.gameplaySceneName))
                    {
                        _speech.Speak("Gameplay scene is not configured for this game.", SpeechPriority.High);
                        return;
                    }

                    _session.SelectMode(item.Mode.modeId);
                    _speech.Speak($"Starting {item.Mode.displayName}.", SpeechPriority.High);
                    await _flow.StartGameplayAsync(_game.gameplaySceneName);
                    break;

                case MenuItemKind.Settings:
                    _speech.Speak("Game settings are not implemented yet.", SpeechPriority.High);
                    break;

                case MenuItemKind.Back:
                    _speech.Speak("Returning to game selection.", SpeechPriority.High);
                    await _flow.ExitGameModuleAsync();
                    break;
            }
        }

        private async System.Threading.Tasks.Task BackAsync()
        {
            if (_flow.IsTransitioning) return;
            _speech.Speak("Returning to game selection.", SpeechPriority.High);
            await _flow.ExitGameModuleAsync();
        }

        private int ResolveInitialIndex()
        {
            if (_items == null || _items.Length == 0) return 0;

            var modeId = _session.SelectedModeId;
            if (string.IsNullOrWhiteSpace(modeId))
                return 0;

            for (int i = 0; i < _items.Length; i++)
            {
                var it = _items[i];
                if (it == null) continue;

                if (it.Kind == MenuItemKind.Mode &&
                    it.Mode != null &&
                    it.Mode.modeId == modeId)
                    return i;
            }

            return 0;
        }
    }
}