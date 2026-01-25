using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Audio.Sequences.Common;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Definitions;
using UnityEngine;

namespace Project.Games.Module
{
    public sealed class GameModuleController : MonoBehaviour
    {
        private IUiAudioOrchestrator _uiAudio;
        private IAppFlowService _flow;
        private ISettingsService _settings;
        private AppSession _session;
        private GameCatalog _catalog;
        private ILocalizationService _loc;

        private IVisualAssistService _va;

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
            var services = AppContext.Services;

            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();
            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();
            _loc = services.Resolve<ILocalizationService>();

            _va = services.Resolve<IVisualAssistService>();
        }

        private void Start()
        {
            if (!LoadSelectedGameOrFail())
                return;

            BuildMenu();
            _index = ResolveInitialIndex();

            RefreshVa(pulse: VaListMoveDirection.None);
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            if (_items == null || _items.Length == 0)
                return;

            switch (action)
            {
                case NavAction.Next:
                    _index = (_index + 1) % _items.Length;
                    RefreshVa(VaListMoveDirection.Next);
                    PlayCurrent();
                    break;

                case NavAction.Previous:
                    _index = (_index - 1 + _items.Length) % _items.Length;
                    RefreshVa(VaListMoveDirection.Previous);
                    PlayCurrent();
                    break;

                case NavAction.Confirm:
                    _ = ConfirmAsync();
                    break;

                case NavAction.Back:
                    _ = BackAsync();
                    break;
            }
        }

        public void OnRepeatRequested()
        {
            if (_items == null || _items.Length == 0) return;
            if (_flow.IsTransitioning) return;

            RefreshVa(VaListMoveDirection.None);
            PlayPrompt();
        }

        private bool LoadSelectedGameOrFail()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
            {
                _ = _flow.ReturnToHubAsync();
                return false;
            }

            _game = _catalog.GetById(_session.SelectedGameId);
            if (_game == null)
            {
                _ = _flow.ReturnToHubAsync();
                return false;
            }

            return true;
        }

        private void BuildMenu()
        {
            var modes = _game != null ? _game.modes : null;
            int modeCount = modes != null ? modes.Length : 0;

            _items = new MenuItem[modeCount + 2];

            for (int i = 0; i < modeCount; i++)
                _items[i] = new MenuItem { Kind = MenuItemKind.Mode, Mode = modes[i] };

            _items[modeCount] = new MenuItem { Kind = MenuItemKind.Settings };
            _items[modeCount + 1] = new MenuItem { Kind = MenuItemKind.Back };
        }

        private void RefreshVa(VaListMoveDirection pulse)
        {
            if (_va == null) return;

            _va.SetHeaderKey("va.screen.game_menu", _game != null ? _game.displayName : "—");
            _va.SetSubHeaderText(DescribeItem(_items[_index]));

            _va.SetIdleHintKey(ResolveControlHintKey());

            _va.ClearTransitioning();

            if (pulse != VaListMoveDirection.None)
                _va.PulseListMove(pulse);
        }

        private void PlayPrompt()
        {
            string hintKey = ResolveControlHintKey();

            var item = _items[_index];
            string currentKey = item.Kind == MenuItemKind.Mode ? "current.mode" : "current.option";
            string currentText = DescribeItem(item);

            _uiAudio.Play(
                UiAudioScope.GameModule,
                ctx => Project.Games.Sequences.GameMenuPromptSequence.Run(
                    ctx,
                    _game.displayName,
                    currentKey,
                    currentText,
                    hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayCurrent()
        {
            var item = _items[_index];
            string key = item.Kind == MenuItemKind.Mode
                ? "current.mode"
                : "current.option";

            _uiAudio.Play(
                UiAudioScope.GameModule,
                ctx => CurrentItemSequence.Run(ctx, key, DescribeItem(item)),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private async System.Threading.Tasks.Task ConfirmAsync()
        {
            if (_flow.IsTransitioning)
                return;

            var item = _items[_index];

            switch (item.Kind)
            {
                case MenuItemKind.Mode:
                    if (item.Mode == null || string.IsNullOrWhiteSpace(_game.gameplaySceneName))
                        return;

                    _session.SelectMode(item.Mode.modeId);

                    _uiAudio.CancelCurrent();
                    _va?.NotifyTransitioning();

                    _uiAudio.Play(
                        UiAudioScope.GameModule,
                        ctx => NavigateToSequence.Run(
                            ctx,
                            "nav.to_gameplay",
                            _game.displayName,
                            ResolveModeName(item.Mode)),
                        SpeechPriority.High,
                        interruptible: false
                    );

                    await _flow.StartGameplayAsync(_game.gameplaySceneName);
                    break;

                case MenuItemKind.Settings:
                    _va?.NotifyTransitioning();

                    _uiAudio.PlayGated(
                        UiAudioScope.GameModule,
                        "nav.to_game_settings",
                        () => _flow.IsTransitioning,
                        0.5f,
                        SpeechPriority.High,
                        _game.displayName
                    );
                    break;

                case MenuItemKind.Back:
                    await BackAsync();
                    break;
            }
        }

        private async System.Threading.Tasks.Task BackAsync()
        {
            if (_flow.IsTransitioning)
                return;

            _va?.NotifyTransitioning();

            _uiAudio.PlayGated(
                UiAudioScope.GameModule,
                "exit.to_game_select",
                () => _flow.IsTransitioning,
                0.5f,
                SpeechPriority.High
            );

            await _flow.ExitGameModuleAsync();
        }

        private string DescribeItem(MenuItem item)
        {
            if (item == null) return "Unknown";

            return item.Kind switch
            {
                MenuItemKind.Mode => ResolveModeName(item.Mode),
                MenuItemKind.Settings => SafeGet("common.settings"),
                MenuItemKind.Back => SafeGet("common.back"),
                _ => "Unknown"
            };
        }

        private string ResolveModeName(GameModeDefinition mode)
        {
            if (mode == null) return "Unknown";

            var id = mode.modeId;
            if (!string.IsNullOrWhiteSpace(id) && _loc != null)
            {
                var key = $"mode.{id}";
                var localized = _loc.Get(key);

                if (!string.IsNullOrWhiteSpace(localized) && localized != key)
                    return localized;
            }

            return !string.IsNullOrWhiteSpace(mode.displayName) ? mode.displayName : "Unknown";
        }

        private int ResolveInitialIndex()
        {
            if (_items == null) return 0;

            var modeId = _session.SelectedModeId;
            if (string.IsNullOrWhiteSpace(modeId))
                return 0;

            for (int i = 0; i < _items.Length; i++)
                if (_items[i].Kind == MenuItemKind.Mode &&
                    _items[i].Mode != null &&
                    _items[i].Mode.modeId == modeId)
                    return i;

            return 0;
        }

        private string ResolveControlHintKey()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.game_menu.touch"
                : "hint.game_menu.keyboard";
        }

        private string SafeGet(string key)
        {
            if (_loc == null || string.IsNullOrWhiteSpace(key)) return key ?? "";
            var s = _loc.Get(key);
            return string.IsNullOrWhiteSpace(s) ? key : s;
        }
    }
}