using Project.Core.App;
using Project.Core.Audio;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Localization;
using Project.Games.Sequences;
using Project.Games.Stats;
using System;
using UnityEngine;

namespace Project.Games.Gameplay
{
    public sealed class GameplayPlaceholderController : MonoBehaviour
    {
        private IAudioFxService _audioFx;
        private IUiAudioOrchestrator _uiAudio;
        private IAppFlowService _flow;
        private ISettingsService _settings;

        private ILocalizationService _loc;

        private AppSession _session;
        private GameCatalog _catalog;

        private IVisualAssistService _va;
        private IGameStatsService _stats;

        private string _gameName = "Unknown";
        private string _modeName = "Unknown";

        private DateTime _runStartUtc;
        private bool _runStarted;

        private void Awake()
        {
            var services = Core.App.AppContext.Services;

            _audioFx = services.Resolve<IAudioFxService>();
            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();

            _loc = services.Resolve<ILocalizationService>();

            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();

            _stats = services.Resolve<IGameStatsService>();
            _va = services.Resolve<IVisualAssistService>();
        }

        private void Start()
        {
            ResolveContextNames();

            StartStatsRun();

            RefreshVa();
            PlayPrompt();
        }

        private void StartStatsRun()
        {
            if (_stats == null || _session == null)
                return;

            _runStartUtc = DateTime.UtcNow;
            _runStarted = true;

            _stats.RecordRunStarted(
                _session.SelectedGameId,
                _session.SelectedModeId
            );
        }

        public void Handle(NavAction action)
        {
            if (action == NavAction.Back)
            {
                _audioFx?.PlayUiCue(UiCueId.Back);
                _ = ReturnAsync();
            }
        }

        public void OnRepeatRequested()
        {
            _audioFx?.PlayUiCue(UiCueId.Repeat);
            RefreshVa();
            PlayPrompt();
        }

        private void ResolveContextNames()
        {
            if (_session == null || _catalog == null)
                return;

            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
                return;

            var game = _catalog.GetById(_session.SelectedGameId);
            if (game == null)
                return;

            _gameName = GameLocalization.GetGameName(_loc, game);
            _modeName = GameLocalization.GetModeName(_loc, _session.SelectedModeId);
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

            FinishStatsRun(completed: false);
            await _flow.ReturnToGameModuleAsync();
        }

        private void FinishStatsRun(bool completed)
        {
            if (!_runStarted || _stats == null || _session == null)
                return;

            var duration = DateTime.UtcNow - _runStartUtc;

            _stats.RecordRunFinished(
                _session.SelectedGameId,
                _session.SelectedModeId,
                duration,
                score: 0,
                completed: completed
            );

            _runStarted = false;
        }
    }
}