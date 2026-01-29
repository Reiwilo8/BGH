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
using Project.Games.Run;
using Project.Games.Sequences;
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

        private IGameRunContextService _runs;
        private IGameRunParametersService _runParams;

        private string _gameName = "Unknown";
        private string _modeName = "Unknown";

        private bool _startRunScheduled;

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

            _va = services.Resolve<IVisualAssistService>();

            try { _runs = services.Resolve<IGameRunContextService>(); }
            catch { _runs = null; }

            try { _runParams = services.Resolve<IGameRunParametersService>(); }
            catch { _runParams = null; }
        }

        private void Start()
        {
            ResolveContextNames();

            EnsurePreparedRunFallback();

            RefreshVa();

            PlayPromptAndStartRunAfterSequence();
        }

        private void EnsurePreparedRunFallback()
        {
            if (_runs == null || _session == null)
                return;

            if (_runs.HasPreparedRun)
                return;

            if (string.IsNullOrWhiteSpace(_session.SelectedGameId) || string.IsNullOrWhiteSpace(_session.SelectedModeId))
                return;

            int? seed = null;
            bool customized = false;

            try
            {
                if (_runParams != null)
                {
                    seed = _runParams.ResolveSeedForNewRun(_session.SelectedGameId);
                    customized = !_runParams.GetUseRandomSeed(_session.SelectedGameId);
                }
            }
            catch
            {
                seed = null;
                customized = false;
            }

            try
            {
                _runs.PrepareRun(
                    gameId: _session.SelectedGameId,
                    modeId: _session.SelectedModeId,
                    seed: seed,
                    initialParameters: null,
                    wereRunSettingsCustomized: customized
                );
            }
            catch { }
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

            PlayPromptOnly();
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

        private void PlayPromptAndStartRunAfterSequence()
        {
            if (_uiAudio == null)
                return;

            string hintKey = ResolveHintKey();
            _startRunScheduled = true;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => PromptThenStartRunSequence(ctx, _gameName, _modeName, hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayPromptOnly()
        {
            if (_uiAudio == null)
                return;

            string hintKey = ResolveHintKey();

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => GameplayPromptSequence.Run(ctx, _gameName, _modeName, hintKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private System.Collections.IEnumerator PromptThenStartRunSequence(
            UiAudioContext ctx,
            string gameName,
            string modeName,
            string hintKey)
        {
            yield return GameplayPromptSequence.Run(ctx, gameName, modeName, hintKey);

            if (ctx == null || ctx.Handle == null || ctx.Handle.IsCancelled)
                yield break;

            StartRunNowForPlaceholder();
        }

        private void StartRunNowForPlaceholder()
        {
            if (!_startRunScheduled)
                return;

            _startRunScheduled = false;

            if (_runs == null)
                return;

            try { _runs.StartRun(); } catch { }
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

            FinishRunForPlaceholderAbort();

            await _flow.ReturnToGameModuleAsync();
        }

        private void FinishRunForPlaceholderAbort()
        {
            if (_runs == null)
                return;

            try
            {
                _runs.FinishRun(
                    reason: GameRunFinishReason.AbortedByUser,
                    completed: false,
                    score: 0
                );
            }
            catch { }
        }
    }
}