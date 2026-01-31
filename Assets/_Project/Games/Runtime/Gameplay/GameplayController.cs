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
    public sealed class GameplayController : MonoBehaviour
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
        private GameplayState _state = GameplayState.Initializing;

        private void Awake()
        {
            var services = AppContext.Services;

            _audioFx = services.Resolve<IAudioFxService>();
            _uiAudio = services.Resolve<IUiAudioOrchestrator>();
            _flow = services.Resolve<IAppFlowService>();
            _settings = services.Resolve<ISettingsService>();
            _loc = services.Resolve<ILocalizationService>();

            _session = services.Resolve<AppSession>();
            _catalog = services.Resolve<GameCatalog>();
            _va = services.Resolve<IVisualAssistService>();

            try { _runs = services.Resolve<IGameRunContextService>(); } catch { _runs = null; }
            try { _runParams = services.Resolve<IGameRunParametersService>(); } catch { _runParams = null; }
        }

        private void Start()
        {
            ResolveContextNames();
            EnsurePreparedRunFallback();

            _state = GameplayState.Intro;

            _va?.SetRootVisible(false);

            RefreshVaForGameplay();

            PlayIntroAndStartRunAfterSequence();
        }

        private void OnDisable()
        {
            _va?.SetRootVisible(true);
        }

        public void Handle(NavAction action)
        {
            if (_state == GameplayState.Exiting)
                return;

            switch (_state)
            {
                case GameplayState.Running:
                    if (action == NavAction.Back)
                        EnterPause();
                    break;

                case GameplayState.Paused:
                    if (action == NavAction.Confirm)
                        ResumeFromPause();
                    else if (action == NavAction.Back)
                        ExitFromPause();
                    break;
            }
        }

        public void OnRepeatRequested()
        {
            if (_state == GameplayState.Paused)
            {
                RefreshVaForPause();
                PlayPausePrompt();
                return;
            }

            if (_state == GameplayState.Running || _state == GameplayState.Intro)
            {
                RefreshVaForGameplay();
                PlayIntroOnly();
            }
        }

        private void ResolveContextNames()
        {
            if (string.IsNullOrWhiteSpace(_session.SelectedGameId))
                return;

            var game = _catalog.GetById(_session.SelectedGameId);
            if (game == null)
                return;

            _gameName = GameLocalization.GetGameName(_loc, game);
            _modeName = GameLocalization.GetModeName(_loc, _session.SelectedModeId);
        }

        private void RefreshVaForGameplay()
        {
            if (_va == null) return;

            _va.SetHeaderKey("va.screen.gameplay", _gameName);
            _va.SetSubHeaderKey("va.mode", _modeName);
            _va.SetIdleHintKey(ResolveGameplayHintKey());
            _va.ClearTransitioning();
        }

        private void ClearVaForGameplayRun()
        {
            if (_va == null) return;

            _va.ClearIdleHint();
            _va.ClearCenter(VaCenterLayer.IdleHint);
            _va.ClearCenter(VaCenterLayer.PlannedSpeech);
            _va.ClearCenter(VaCenterLayer.Gesture);
            _va.ClearCenter(VaCenterLayer.Transition);
        }

        private void RefreshVaForPause()
        {
            if (_va == null) return;

            _va.ClearTransitioning();

            _va.SetHeaderKey("va.screen.paused");
            _va.SetSubHeaderKey("va.game_and_mode", _gameName, _modeName);

            string hint = ResolvePauseHintKey();
            _va.SetCenterKey(VaCenterLayer.PlannedSpeech, hint);
            _va.SetIdleHintKey(hint);
        }

        private string ResolveGameplayHintKey()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.gameplay.touch"
                : "hint.gameplay.keyboard";
        }

        private string ResolvePauseHintKey()
        {
            var mode = _settings.Current.controlHintMode;
            if (mode == ControlHintMode.Auto)
                mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode();

            return mode == ControlHintMode.Touch
                ? "hint.pause.touch"
                : "hint.pause.keyboard";
        }

        private void PlayIntroAndStartRunAfterSequence()
        {
            if (_uiAudio == null)
                return;

            _startRunScheduled = true;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => IntroThenStartRunSequence(ctx),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void PlayIntroOnly()
        {
            if (_uiAudio == null)
                return;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => GameplayPromptSequence.Run(ctx, _audioFx),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private System.Collections.IEnumerator IntroThenStartRunSequence(UiAudioContext ctx)
        {
            yield return GameplayPromptSequence.Run(ctx, _audioFx);

            if (ctx?.Handle == null || ctx.Handle.IsCancelled)
                yield break;

            StartRunNow();
        }

        private void StartRunNow()
        {
            if (!_startRunScheduled || _runs == null)
                return;

            _startRunScheduled = false;
            _state = GameplayState.Running;

            _va?.SetRootVisible(false);

            ClearVaForGameplayRun();

            try { _runs.StartRun(); } catch { }
        }

        private void EnterPause()
        {
            if (_state != GameplayState.Running)
                return;

            _state = GameplayState.Paused;

            _audioFx.PlayCommonGameSound(CommonGameSoundId.Pause);

            try { _runs?.PauseRun(); } catch { _audioFx.PlayUiCue(UiCueId.Error); }

            _va?.SetRootVisible(true);

            RefreshVaForPause();
            PlayPausePrompt();
        }

        private void ResumeFromPause()
        {
            if (_state != GameplayState.Paused)
                return;

            try { _uiAudio?.CancelCurrent(); } catch { }

            _audioFx?.PlayCommonGameSound(CommonGameSoundId.Unpause);

            _state = GameplayState.Running;

            try { _runs?.ResumeRun(); } catch { }

            _va?.SetRootVisible(false);

            ClearVaForGameplayRun();
        }

        private void ExitFromPause()
        {
            _audioFx?.PlayUiCue(UiCueId.Back);
            _ = ReturnAsync();
        }

        private void PlayPausePrompt()
        {
            if (_uiAudio == null)
                return;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => PausePromptSequence.Run(ctx, ResolvePauseHintKey()),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private async System.Threading.Tasks.Task ReturnAsync()
        {
            if (_flow.IsTransitioning)
                return;

            _state = GameplayState.Exiting;

            _va?.SetRootVisible(true);

            _va.NotifyTransitioning();

            _uiAudio.PlayGated(
                UiAudioScope.Gameplay,
                "exit.to_game_menu",
                () => _flow.IsTransitioning,
                0.5f,
                SpeechPriority.High
            );

            FinishRunAbort();

            await _flow.ReturnToGameModuleAsync();
        }

        private void FinishRunAbort()
        {
            try
            {
                _runs?.FinishRun(
                    reason: GameRunFinishReason.AbortedByUser,
                    completed: false,
                    score: 0
                );
            }
            catch { }
        }

        private void EnsurePreparedRunFallback()
        {
            if (_runs == null || _runs.HasPreparedRun)
                return;

            if (string.IsNullOrWhiteSpace(_session.SelectedGameId) ||
                string.IsNullOrWhiteSpace(_session.SelectedModeId))
                return;

            int? seed = null;
            bool useRandomSeed = true;

            try
            {
                if (_runParams != null)
                {
                    useRandomSeed = _runParams.GetUseRandomSeed(_session.SelectedGameId);
                    seed = _runParams.ResolveSeedForNewRun(_session.SelectedGameId);
                }
            }
            catch { }

            var initialParams = GameRunInitialParametersBuilder.Build(
                settings: _settings,
                useRandomSeed: useRandomSeed,
                seedValue: seed
            );

            try
            {
                _runs.PrepareRun(
                    gameId: _session.SelectedGameId,
                    modeId: _session.SelectedModeId,
                    seed: seed,
                    initialParameters: initialParams,
                    wereRunSettingsCustomized: !useRandomSeed
                );
            }
            catch { }
        }
    }
}