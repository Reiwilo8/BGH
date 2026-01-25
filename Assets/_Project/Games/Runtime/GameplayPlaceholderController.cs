using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Sequences;
using UnityEngine;

namespace Project.Games.Gameplay
{
    public sealed class GameplayPlaceholderController : MonoBehaviour
    {
        private IUiAudioOrchestrator _uiAudio;
        private IAppFlowService _flow;
        private ISettingsService _settings;

        private AppSession _session;
        private GameCatalog _catalog;

        private IVisualAssistService _va;

        private string _gameName = "Unknown";
        private string _modeName = "Unknown";

        private void Awake()
        {
            var services = AppContext.Services;
            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();

            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();

            _va = services.Resolve<IVisualAssistService>();
        }

        private void Start()
        {
            ResolveContextNames();
            RefreshVa();
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            if (action == NavAction.Back)
                _ = ReturnAsync();
        }

        public void OnRepeatRequested()
        {
            RefreshVa();
            PlayPrompt();
        }

        private void ResolveContextNames()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId) || _catalog == null)
                return;

            var game = _catalog.GetById(_session.SelectedGameId);
            if (game == null)
                return;

            if (!string.IsNullOrWhiteSpace(game.displayName))
                _gameName = game.displayName;

            if (string.IsNullOrWhiteSpace(_session.SelectedModeId))
                return;

            var modeId = _session.SelectedModeId;

            Project.Core.Localization.ILocalizationService loc = null;
            try { loc = AppContext.Services.Resolve<Project.Core.Localization.ILocalizationService>(); }
            catch { }

            if (loc != null && !string.IsNullOrWhiteSpace(modeId))
            {
                var key = $"mode.{modeId}";
                var localized = loc.Get(key);

                if (!string.IsNullOrWhiteSpace(localized) && localized != key)
                {
                    _modeName = localized;
                    return;
                }
            }

            var mode = game.GetMode(modeId);
            if (mode != null && !string.IsNullOrWhiteSpace(mode.displayName))
                _modeName = mode.displayName;
        }

        private void RefreshVa()
        {
            _va?.SetHeaderKey("va.screen.gameplay", _gameName);
            _va?.SetSubHeaderKey("va.mode", _modeName);

            var hintKey = ResolveHintKey();
            _va?.SetIdleHintKey(hintKey);
            _va?.ClearTransitioning();
        }

        private string ResolveHintKey()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.gameplay.touch"
                : "hint.gameplay.keyboard";
        }

        private void PlayPrompt()
        {
            string hintKey = ResolveHintKey();

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => GameplayPromptSequence.Run(ctx, _gameName, _modeName, hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private async System.Threading.Tasks.Task ReturnAsync()
        {
            if (_flow.IsTransitioning)
                return;

            _va?.NotifyTransitioning();

            _uiAudio.PlayGated(
                UiAudioScope.Gameplay,
                "exit.to_game_menu",
                () => _flow.IsTransitioning,
                0.5f,
                SpeechPriority.High
            );

            await _flow.ReturnToGameModuleAsync();
        }
    }
}