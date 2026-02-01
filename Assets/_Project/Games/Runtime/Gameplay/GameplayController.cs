using Project.Core.App;
using Project.Core.Audio;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.VisualAssist;
using Project.Games.Catalog;
using Project.Games.Gameplay.Contracts;
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

        private IGameplayGame _game;
        private IGameplayInputHandler _gameInput;
        private IGameplayDirection4Handler _gameDir4;
        private bool _gameInitialized;
        private bool _gameFinishedHandled;

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
            _gameInitialized = false;
            _gameFinishedHandled = false;
            _startRunScheduled = false;

            ResolveContextNames();
            EnsurePreparedRunFallback();

            ResolveGameplayGame();

            _state = GameplayState.Intro;

            _va?.SetRootVisible(false);

            RefreshVaForGameplay();

            PlayIntroAndStartRunAfterSequence();
        }

        private void OnDisable()
        {
            _va?.SetRootVisible(true);

            UnsubscribeGameFinished();

            TryFinishRunOnDisable();

            TryStopGameSafe();
        }

        public void Handle(NavAction action)
        {
            if (_state == GameplayState.Exiting)
                return;

            switch (_state)
            {
                case GameplayState.Running:
                    if (action == NavAction.Back)
                    {
                        EnterPause(fromIntro: false);
                        return;
                    }

                    _gameInput?.Handle(action);
                    break;

                case GameplayState.Paused:
                    if (action == NavAction.Confirm)
                        ResumeFromPause();
                    else if (action == NavAction.Back)
                        ExitFromPause();
                    break;

                case GameplayState.Intro:
                    if (action == NavAction.Back)
                    {
                        EnterPause(fromIntro: true);
                    }
                    break;
            }
        }

        public void Handle(NavDirection4 direction)
        {
            if (_state == GameplayState.Exiting)
                return;

            if (_state != GameplayState.Running)
                return;

            _gameDir4?.Handle(direction);
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

        private void ResolveGameplayGame()
        {
#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
#endif
            if (all == null) return;

            _game = null;
            _gameInput = null;
            _gameDir4 = null;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is IGameplayGame g)
                {
                    _game = g;

                    _gameInput = all[i] as IGameplayInputHandler;
                    _gameDir4 = all[i] as IGameplayDirection4Handler;

                    break;
                }
            }
        }

        private void ResolveContextNames()
        {
            if (_session == null || _catalog == null || _loc == null)
                return;

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

            string hintKey = ResolvePauseHintKey();
            _va.SetCenterKey(VaCenterLayer.PlannedSpeech, hintKey);
            _va.SetIdleHintKey(hintKey);
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

            if (_game == null)
            {
                _audioFx?.PlayUiCue(UiCueId.Error);
                _ = ReturnAsync(abortRun: true);
                return;
            }

            _state = GameplayState.Running;

            _va?.SetRootVisible(false);

            ClearVaForGameplayRun();

            bool started = false;
            try { started = _runs.StartRun(); } catch { started = false; }

            if (!started)
            {
                _audioFx?.PlayUiCue(UiCueId.Error);
                _ = ReturnAsync(abortRun: true);
                return;
            }

            InitializeGameIfNeeded();
            TryStartGameSafe();
        }

        private void InitializeGameIfNeeded()
        {
            if (_game == null || _gameInitialized || _runs == null)
                return;

            var ctx = _runs.Current;

            var gameCtx = new GameplayGameContext(
                gameId: ctx.GameId,
                modeId: ctx.ModeId,
                runId: ctx.RunId,
                seed: ctx.Seed,
                initialParameters: ctx.InitialParameters
            );

            try
            {
                _game.Initialize(gameCtx);
                SubscribeGameFinished();
                _gameInitialized = true;
            }
            catch
            {
                _gameInitialized = false;
            }
        }

        private void SubscribeGameFinished()
        {
            if (_game == null) return;

            _game.GameFinished -= OnGameFinished;
            _game.GameFinished += OnGameFinished;
        }

        private void UnsubscribeGameFinished()
        {
            if (_game == null) return;
            _game.GameFinished -= OnGameFinished;
        }

        private void TryStartGameSafe()
        {
            if (_game == null) return;
            try { _game.StartGame(); } catch { }
        }

        private void TryStopGameSafe()
        {
            if (_game == null) return;
            try { _game.StopGame(); } catch { }
        }

        private void EnterPause(bool fromIntro)
        {
            if (_state != GameplayState.Running && _state != GameplayState.Intro)
                return;

            _state = GameplayState.Paused;

            _audioFx?.PlayCommonGameSound(CommonGameSoundId.Pause);

            if (!fromIntro)
            {
                try { _game?.PauseGame(); } catch { }
                try { _runs?.PauseRun(); } catch { }
            }

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

            bool runStarted = false;
            try { runStarted = _runs != null && _runs.HasStartedRun; } catch { runStarted = false; }

            _state = runStarted ? GameplayState.Running : GameplayState.Intro;

            if (runStarted)
            {
                try { _runs?.ResumeRun(); } catch { }
                try { _game?.ResumeGame(); } catch { }

                _va?.SetRootVisible(false);
                ClearVaForGameplayRun();
            }
            else
            {
                _va?.SetRootVisible(false);
                RefreshVaForGameplay();
                PlayIntroOnly();
            }
        }

        private void ExitFromPause()
        {
            _audioFx?.PlayUiCue(UiCueId.Back);

            try { _uiAudio?.CancelCurrent(); } catch { }

            TryStopGameSafe();

            _ = ReturnAsync(abortRun: true);
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

        private void OnGameFinished(GameplayGameResult result)
        {
            if (_gameFinishedHandled)
                return;

            _gameFinishedHandled = true;

            if (_state == GameplayState.Exiting)
                return;

            _state = GameplayState.Exiting;

            try { _uiAudio?.CancelCurrent(); } catch { }

            _va?.SetRootVisible(true);

            var (reason, completed) = MapFinish(result.Reason);

            try
            {
                _runs?.FinishRun(
                    reason: reason,
                    completed: completed,
                    score: result.Score
                );
            }
            catch { }

            _ = ReturnAsync(abortRun: false);
        }

        private static (GameRunFinishReason reason, bool completed) MapFinish(GameplayGameFinishReason r)
        {
            return r switch
            {
                GameplayGameFinishReason.Completed => (GameRunFinishReason.Completed, true),
                GameplayGameFinishReason.Failed => (GameRunFinishReason.Failed, false),
                GameplayGameFinishReason.Quit => (GameRunFinishReason.Quit, false),
                _ => (GameRunFinishReason.Unknown, false)
            };
        }

        private async System.Threading.Tasks.Task ReturnAsync(bool abortRun)
        {
            if (_flow == null || _flow.IsTransitioning)
                return;

            _state = GameplayState.Exiting;

            _va?.SetRootVisible(true);

            _va?.NotifyTransitioning();

            _uiAudio?.PlayGated(
                UiAudioScope.Gameplay,
                "exit.to_game_menu",
                () => _flow.IsTransitioning,
                0.5f,
                SpeechPriority.High
            );

            if (abortRun)
                FinishRunAbort();

            try
            {
                await _flow.ReturnToGameModuleAsync();
            }
            catch { }
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

        private void TryFinishRunOnDisable()
        {
            if (_runs == null) return;
            if (_state == GameplayState.Exiting) return;

            bool prepared = false;
            bool started = false;
            try
            {
                prepared = _runs.HasPreparedRun;
                started = _runs.HasStartedRun;
            }
            catch { }

            if (!prepared || !started)
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

        private void EnsurePreparedRunFallback()
        {
            if (_runs == null || _runs.HasPreparedRun)
                return;

            if (_session == null ||
                string.IsNullOrWhiteSpace(_session.SelectedGameId) ||
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