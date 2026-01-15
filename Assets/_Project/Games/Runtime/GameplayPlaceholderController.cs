using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Core.Speech;
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
        }

        private void Start()
        {
            ResolveContextNames();
            PlayPrompt();
        }

        public void Handle(NavAction action)
        {
            if (action == NavAction.Back)
                _ = ReturnAsync();
        }

        public void OnRepeatRequested()
        {
            PlayPrompt();
        }

        private void ResolveContextNames()
        {
            if (!string.IsNullOrWhiteSpace(_session.SelectedGameId) && _catalog != null)
            {
                var game = _catalog.GetById(_session.SelectedGameId);
                if (game != null && !string.IsNullOrWhiteSpace(game.displayName))
                {
                    _gameName = game.displayName;

                    if (!string.IsNullOrWhiteSpace(_session.SelectedModeId))
                    {
                        var mode = game.GetMode(_session.SelectedModeId);
                        if (mode != null && !string.IsNullOrWhiteSpace(mode.displayName))
                            _modeName = mode.displayName;
                    }
                }
            }
        }

        private void PlayPrompt()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            string hintKey = mode == ControlHintMode.Touch
                ? "hint.gameplay.touch"
                : "hint.gameplay.keyboard";

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