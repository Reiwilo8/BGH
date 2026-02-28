using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;
using Project.Core.Haptics;
using Project.Core.Input;
using Project.Core.Input.Motion;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.Visual.Games.Fishing;
using Project.Games.Gameplay.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Project.Games.Fishing
{
    public sealed class FishingGame : MonoBehaviour,
        IGameplayGame,
        IGameplayMotionHandler,
        IGameplayRuntimeStatsProvider,
        IGameplayRepeatHandler
    {
        private const string GameId = "fishing";
        private const string GameTable = "Game_Fishing";

        private const string KeyHintObjective = "hint";
        private const string KeyHintTouch = "hint.touch";
        private const string KeyHintKeyboard = "hint.keyboard";

        private const float TutorialPostOnboardingDelaySeconds = 1.0f;
        private const float TutorialSpeechStableFalseSeconds = 0.50f;

        private const string PBaseBiteWaitMin = "fishing.biteWaitMinSeconds";
        private const string PBaseBiteWaitMax = "fishing.biteWaitMaxSeconds";
        private const string PBaseReactionWindowBase = "fishing.reactionWindowBaseSeconds";

        private const string PCatchDistanceBase = "fishing.catchDistanceBase";
        private const string PSpawnDistanceBase = "fishing.spawnDistanceBase";
        private const string PSpawnDistanceJitter = "fishing.spawnDistanceJitter";

        private const string PTensionMaxTicks = "fishing.tensionMaxTicks";

        private const string PActionMinSeconds = "fishing.actionMinSeconds";
        private const string PActionMaxSeconds = "fishing.actionMaxSeconds";

        private const string PMoveLateralSpeedMin = "fishing.moveLateralSpeedMin";
        private const string PMoveLateralSpeedMax = "fishing.moveLateralSpeedMax";

        private const string PBurstForwardSpeedMin = "fishing.burstForwardSpeedMin";
        private const string PBurstForwardSpeedMax = "fishing.burstForwardSpeedMax";

        private const string PFailGraceSeconds = "fishing.failGraceSeconds";

        private const string PLoosenDistancePenaltyMin = "fishing.loosenDistancePenaltyMin";
        private const string PLoosenDistancePenaltyMax = "fishing.loosenDistancePenaltyMax";

        private const string PFatigueGainOnCorrect = "fishing.fatigueGainOnCorrect";
        private const string PFatigueLossOnWrong = "fishing.fatigueLossOnWrong";
        private const string PFatigueLossOnLoosen = "fishing.fatigueLossOnLoosen";

        private const string PDifficultyScale = "fishing.difficultyScale";
        private const string PTargetFishCount = "fishing.targetFishCount";
        private const string PAggressionMin = "fishing.aggressionMin";
        private const string PAggressionMax = "fishing.aggressionMax";
        private const string PResistanceMin = "fishing.resistanceMin";
        private const string PResistanceMax = "fishing.resistanceMax";
        private const string PReactionWindowScale = "fishing.reactionWindowScale";

        private const string SfxRodCast = "RodCast";
        private const string SfxMove = "Move";
        private const string SfxTurn = "Turn";
        private const string SfxHookBite = "HookBite";
        private const string SfxBurst = "Burst";
        private const string SfxIdle = "Idle";
        private const string SfxReel = "Reel";
        private const string SfxLoosen = "Loosen";
        private const string SfxLineBreak = "LineBreak";
        private const string SfxSplashing = "Splashing";
        private const string SfxRodPullOut = "RodPullOut";
        private const string SfxCatch = "Catch";
        private const string SfxWin = "Win";
        private const string SfxPull = "Pull";

        private const float BoardForwardMax = 1.0f;
        private const float BoardSideHalf = 0.5f;

        private const float PostCatchDelaySeconds = 0.55f;
        private const float PostWinExitDelaySeconds = 1.10f;
        private const float PostRoundResetDelaySeconds = 0.55f;

        private const float PreWinSoundDelaySeconds = 2.0f;
        private const float PostWinSoundDelaySeconds = 2.0f;

        private const float TiltSignalHoldSeconds = 0.35f;
        private const float TiltForgivenessSeconds = 0.18f;

        private const float FishAudioFarVolume = 0.35f;

        private const float LinePanMax = 1.00f;

        private const float CenterSnapStrength = 0.95f;

        private const float TurnBStartSeconds = 1.590f;
        private const float TurnBEndSeconds = 2.078f;

        private const float TurnDoubleGapSeconds = 0.03f;
        private const float AfterDoubleTurnToMoveGapSeconds = 0.22f;

        private const float FishCueTrimLeadSeconds = 0.0f;

        private const float BurstLoopStart = 0.000f;
        private const float BurstLoopEnd = 3.42f;

        private const float BurstVolumeBoost = 1.35f;

        private const float MinIdleReminderSeconds = 4.20f;
        private const float MaxIdleReminderSeconds = 7.20f;

        private const float MinFishCueGapEasy = 2.80f;
        private const float MaxFishCueGapEasy = 4.30f;

        private const float MinFishCueGapMedium = 1.85f;
        private const float MaxFishCueGapMedium = 3.00f;

        private const float MinFishCueGapHard = 1.25f;
        private const float MaxFishCueGapHard = 2.15f;

        private const float AfterFishCueMinGap = 0.22f;

        private const float HookBiteStopDelaySeconds = 0.12f;

        private const float MinMoveReactionWindowSeconds = 0.95f;

        private const float MinBurstReactionWindowSeconds = 1.75f;
        private const float BurstStopBeforeDeadlineSeconds = 0.28f;

        private const float BiteOffMinSeconds = 0.22f;
        private const float BiteOffMaxSeconds = 0.45f;
        private const float BiteOnRandMinMul = 0.75f;
        private const float BiteOnRandMaxMul = 1.35f;

        private const int MaxIdleReelsPerIdle = 3;

        private const float MinIdleHoldSeconds = 0.55f;

        private const float PostBurstPullGapSeconds = 0.28f;

        private const float TurnSequenceTotalSeconds =
            (TurnBEndSeconds - TurnBStartSeconds) + TurnDoubleGapSeconds + (TurnBEndSeconds - TurnBStartSeconds);

        private const float PostBurstLoudnessBlendSeconds = 0.55f;

        private const float IdleCueClipSeconds = 1.000f;
        private const float MoveCueClipSeconds = 1.000f;

        private const float PostCueSafetyGapSeconds = 0.06f;

        private const float ActionOneShotMinRestartGapSeconds = 0.08f;

        private const float VisualPushMinIntervalSeconds = 0.05f;
        private const float VisualPushIdleIntervalSeconds = 0.20f;
        private const float VisualDistanceEpsilon = 0.0025f;
        private const float VisualLateralEpsilon = 0.0035f;

        private IAudioFxService _audioFx;
        private IHapticsService _haptics;
        private IVisualModeService _visualMode;
        private IFishingVisualDriver _visual;

        private IUiAudioOrchestrator _uiAudio;
        private ILocalizationService _loc;
        private ISettingsService _settings;
        private ISpeechService _speech;

        private bool _initialized;
        private bool _running;
        private bool _paused;
        private bool _finishQueued;

        private GameplayGameContext _ctx;

        private enum Phase
        {
            Idle,
            Waiting,
            Biting,
            Reeling,
            RoundEnd,
            Win
        }

        private enum FishAction
        {
            Idle,
            Move,
            Burst
        }

        private Phase _phase = Phase.Idle;

        private float _biteWaitMin;
        private float _biteWaitMax;
        private float _reactionWindowBaseSeconds;

        private float _catchDistanceBase;
        private float _spawnDistanceBase;
        private float _spawnDistanceJitter;

        private float _difficultySpawnBias;

        private float _difficultyScale;
        private int _targetFishCount;
        private float _aggressionMin;
        private float _aggressionMax;
        private float _resistanceMin;
        private float _resistanceMax;
        private float _reactionWindowScale;

        private int _tensionMaxTicks;
        private int _tensionHapticsTick;

        private float _actionMinSeconds;
        private float _actionMaxSeconds;

        private float _moveLateralSpeedMin;
        private float _moveLateralSpeedMax;

        private float _burstForwardSpeedMin;
        private float _burstForwardSpeedMax;

        private float _failGraceSeconds;

        private float _loosenDistancePenaltyMin;
        private float _loosenDistancePenaltyMax;

        private float _fatigueGainOnCorrect;
        private float _fatigueLossOnWrong;
        private float _fatigueLossOnLoosen;

        private float _gameTime;
        private float _phaseStartAtT;

        private float _nextBiteAtT;

        private bool _biteIsOn;
        private float _biteSwitchAtT;

        private float _fishDistance;
        private float _fishLateral;

        private float _aggression;
        private float _resistance;

        private float _catchDistance;

        private FishAction _fishAction;
        private float _nextActionResampleAtT;

        private float _moveStartAtT;
        private int _moveDir;
        private float _moveSpeed;

        private float _burstSpeed;

        private bool _idleSfxPlayedThisIdle;
        private float _nextIdleReminderAtT;

        private int _tiltDir;
        private float _tiltHoldUntilT;

        private int _tensionTicks;
        private float _fatigue;

        private int _pullsToCatchTarget;
        private int _pullsToCatchProgress;
        private float _progressStartDistance;

        private int _idleReelsInCurrentIdle;

        private float _reactionDeadlineAtT;
        private float _ignoreInputUntilT;

        private float _suppressIdleSfxUntilT;

        private AudioFxHandle _hookBiteLoop;
        private float _stopHookBiteAtT;

        private AudioFxHandle _burstLoop;
        private float _stopBurstLoopAtT;

        private AudioFxHandle _splashLoop;

        private AudioFxHandle _actionOneShot;
        private string _actionOneShotSoundId;
        private float _actionOneShotStartedAtT;

        private AudioFxHandle _lineBreakOneShot;

        private AudioFxHandle _fishMoveOneShot;
        private AudioFxHandle _turnBOneShot;
        private AudioFxHandle _idleCueOneShot;
        private AudioFxHandle _catchOneShot;

        private float _idleSfxEndsAtT;
        private float _moveSfxEndsAtT;

        private int _pendingTurnBPlays;
        private float _nextTurnBAtT;
        private float _turnPan;

        private float _nextFishSfxCheckAtT;

        private float _moveSfxCue1AtT;
        private float _moveSfxCue2AtT;
        private int _moveSfxCueIndex;

        private float _afterTurnLockUntilT;

        private HapticsHandle _hapticsContinuous;

        private int _caught;
        private int _lostTimeout;
        private int _lostLine;
        private float _maxTension;
        private float _totalTime;

        private float _preBurstFishVolume01;
        private float _postBurstFishVolumeMul;
        private float _postBurstFishVolumeMulUntilT;

        private Coroutine _finishCo;

        private bool _tutorialOnboardingSpoken;
        private Coroutine _tutorialOnboardingCo;

        private bool _visualAssistActive;
        private bool _visualDirty;
        private float _nextVisualPushAtT;

        private FishingVisualState _lastPushedVisual;
        private bool _hasLastPushedVisual;

        public event Action<GameplayGameResult> GameFinished;

        private bool IsVisualAssistEnabled
        {
            get
            {
                try { return _visualMode != null && _visualMode.Mode == VisualMode.VisualAssist; }
                catch { return false; }
            }
        }

        private void EnsureVisualDriverResolvedIfNeeded()
        {
            if (!_visualAssistActive)
                return;

            if (_visual != null)
                return;

            ResolveVisual();
        }

        private void ReleaseVisualDriverIfAny()
        {
            if (_visual == null)
                return;

            try { _visual.SetVisible(false); } catch { }
            try { _visual.Reset(); } catch { }
            _visual = null;
        }

        private void SyncVisualModeState(bool forcePush)
        {
            bool want = IsVisualAssistEnabled;
            if (want == _visualAssistActive && !forcePush)
                return;

            _visualAssistActive = want;

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                _visualDirty = true;
                if (forcePush) TryPushVisualState(force: true);
            }
            else
            {
                ReleaseVisualDriverIfAny();
                _visualDirty = false;
                _hasLastPushedVisual = false;
                _nextVisualPushAtT = 0f;
            }
        }

        private void MarkVisualDirty()
        {
            if (!_visualAssistActive) return;
            _visualDirty = true;
        }

        private void RequestVisualPush(bool force)
        {
            if (!_visualAssistActive) return;
            MarkVisualDirty();
            TryPushVisualState(force);
        }

        public void Initialize(GameplayGameContext context)
        {
            _ctx = context;

            ResolveServices();

            _visualAssistActive = IsVisualAssistEnabled;
            if (_visualAssistActive)
                ResolveVisual();
            else
                _visual = null;

            _biteWaitMin = GetRequiredFloat(_ctx.InitialParameters, PBaseBiteWaitMin, min: 0.10f, max: 30f);
            _biteWaitMax = GetRequiredFloat(_ctx.InitialParameters, PBaseBiteWaitMax, min: 0.10f, max: 30f);
            if (_biteWaitMax < _biteWaitMin)
                throw new InvalidOperationException($"FishingGame.Initialize: {PBaseBiteWaitMax} < {PBaseBiteWaitMin}.");

            _reactionWindowBaseSeconds = GetRequiredFloat(_ctx.InitialParameters, PBaseReactionWindowBase, min: 0.10f, max: 10f);

            _catchDistanceBase = GetRequiredFloat(_ctx.InitialParameters, PCatchDistanceBase, min: 0.001f, max: 0.50f);

            _spawnDistanceBase = GetRequiredFloat(_ctx.InitialParameters, PSpawnDistanceBase, min: 0.01f, max: 0.99f);
            _spawnDistanceJitter = GetRequiredFloat(_ctx.InitialParameters, PSpawnDistanceJitter, min: 0.00f, max: 0.49f);

            _difficultySpawnBias = 0.04f;

            float spawnWorstMin = _spawnDistanceBase - Mathf.Abs(_spawnDistanceJitter) + Mathf.Min(0f, _difficultySpawnBias);
            float spawnWorstMax = _spawnDistanceBase + Mathf.Abs(_spawnDistanceJitter) + Mathf.Max(0f, _difficultySpawnBias);
            if (spawnWorstMin <= 0.001f || spawnWorstMax >= 0.999f)
                throw new InvalidOperationException("FishingGame.Initialize: spawnDistanceBase/jitter can produce out-of-bounds spawnDistance.");

            _difficultyScale = GetRequiredFloat(_ctx.InitialParameters, PDifficultyScale, min: 0.10f, max: 10.0f);
            _targetFishCount = GetRequiredInt(_ctx.InitialParameters, PTargetFishCount, min: 1, max: 999);

            _aggressionMin = GetRequiredFloat(_ctx.InitialParameters, PAggressionMin, min: 0.0f, max: 1.0f);
            _aggressionMax = GetRequiredFloat(_ctx.InitialParameters, PAggressionMax, min: 0.0f, max: 1.0f);
            if (_aggressionMax < _aggressionMin)
                throw new InvalidOperationException("FishingGame.Initialize: aggressionMax < aggressionMin.");

            _resistanceMin = GetRequiredFloat(_ctx.InitialParameters, PResistanceMin, min: 0.10f, max: 10.0f);
            _resistanceMax = GetRequiredFloat(_ctx.InitialParameters, PResistanceMax, min: 0.10f, max: 10.0f);
            if (_resistanceMax < _resistanceMin)
                throw new InvalidOperationException("FishingGame.Initialize: resistanceMax < resistanceMin.");

            _reactionWindowScale = GetRequiredFloat(_ctx.InitialParameters, PReactionWindowScale, min: 0.10f, max: 10.0f);

            _tensionMaxTicks = GetRequiredInt(_ctx.InitialParameters, PTensionMaxTicks, min: 1, max: 12);
            _tensionHapticsTick = Mathf.Clamp(_tensionMaxTicks - 1, 0, _tensionMaxTicks);

            _actionMinSeconds = GetRequiredFloat(_ctx.InitialParameters, PActionMinSeconds, min: 0.10f, max: 30.0f);
            _actionMaxSeconds = GetRequiredFloat(_ctx.InitialParameters, PActionMaxSeconds, min: 0.10f, max: 30.0f);
            if (_actionMaxSeconds < _actionMinSeconds)
                throw new InvalidOperationException("FishingGame.Initialize: actionMaxSeconds < actionMinSeconds.");

            _moveLateralSpeedMin = GetRequiredFloat(_ctx.InitialParameters, PMoveLateralSpeedMin, min: 0.01f, max: 2.50f);
            _moveLateralSpeedMax = GetRequiredFloat(_ctx.InitialParameters, PMoveLateralSpeedMax, min: 0.01f, max: 2.50f);
            if (_moveLateralSpeedMax < _moveLateralSpeedMin)
                throw new InvalidOperationException("FishingGame.Initialize: moveLateralSpeedMax < moveLateralSpeedMin.");

            _burstForwardSpeedMin = GetRequiredFloat(_ctx.InitialParameters, PBurstForwardSpeedMin, min: 0.01f, max: 5.0f);
            _burstForwardSpeedMax = GetRequiredFloat(_ctx.InitialParameters, PBurstForwardSpeedMax, min: 0.01f, max: 5.0f);
            if (_burstForwardSpeedMax < _burstForwardSpeedMin)
                throw new InvalidOperationException("FishingGame.Initialize: burstForwardSpeedMax < burstForwardSpeedMin.");

            _failGraceSeconds = GetRequiredFloat(_ctx.InitialParameters, PFailGraceSeconds, min: 0.00f, max: 5.0f);

            _loosenDistancePenaltyMin = GetRequiredFloat(_ctx.InitialParameters, PLoosenDistancePenaltyMin, min: 0.0f, max: 0.50f);
            _loosenDistancePenaltyMax = GetRequiredFloat(_ctx.InitialParameters, PLoosenDistancePenaltyMax, min: 0.0f, max: 0.50f);
            if (_loosenDistancePenaltyMax < _loosenDistancePenaltyMin)
                throw new InvalidOperationException("FishingGame.Initialize: loosenDistancePenaltyMax < loosenDistancePenaltyMin.");

            _fatigueGainOnCorrect = GetRequiredFloat(_ctx.InitialParameters, PFatigueGainOnCorrect, min: 0.0f, max: 1.0f);
            _fatigueLossOnWrong = GetRequiredFloat(_ctx.InitialParameters, PFatigueLossOnWrong, min: 0.0f, max: 1.0f);
            _fatigueLossOnLoosen = GetRequiredFloat(_ctx.InitialParameters, PFatigueLossOnLoosen, min: 0.0f, max: 1.0f);

            ResetRuntimeState(fullReset: true);

            _initialized = true;
            _running = false;
            _paused = false;
            _finishQueued = false;

            StopTutorialOnboardingRoutine();
            _tutorialOnboardingSpoken = false;

            _visualDirty = true;
            _hasLastPushedVisual = false;
            _nextVisualPushAtT = 0f;

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualSetVisible(false);
                TryVisualReset();
                TryVisualSetPaused(false);
                RequestVisualPush(force: true);
            }
            else
            {
                ReleaseVisualDriverIfAny();
            }
        }

        public void StartGame()
        {
            if (!_initialized) return;

            _running = true;
            _paused = false;
            _finishQueued = false;

            StopFinishRoutineIfAny();
            StopAllHandlesAndClear();

            ResetRuntimeState(fullReset: true);
            EnterIdle();

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualReset();
                TryVisualSetPaused(false);
                TryVisualSetVisible(true);
                RequestVisualPush(force: true);
            }
            else
            {
                ReleaseVisualDriverIfAny();
            }

            if (string.Equals(_ctx.ModeId, "tutorial", StringComparison.OrdinalIgnoreCase) && !_tutorialOnboardingSpoken)
            {
                _tutorialOnboardingSpoken = true;
                StopTutorialOnboardingRoutine();
                _tutorialOnboardingCo = StartCoroutine(TutorialOnboardingRoutine());
            }
        }

        public void StopGame()
        {
            _running = false;
            _paused = false;

            StopTutorialOnboardingRoutine();

            StopFinishRoutineIfAny();
            StopAllHandlesAndClear();

            _phase = Phase.Idle;

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualSetPaused(false);
                TryVisualSetVisible(false);
                RequestVisualPush(force: true);
            }
            else
            {
                ReleaseVisualDriverIfAny();
            }
        }

        public void PauseGame()
        {
            _paused = true;
            PauseKnownHandles();

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualSetPaused(true);
                RequestVisualPush(force: true);
            }
        }

        public void ResumeGame()
        {
            _paused = false;
            ResumeKnownHandles();

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualSetPaused(false);
                RequestVisualPush(force: true);
            }
        }

        private void Update()
        {
            if (!_initialized || !_running || _paused)
                return;

            if (_finishQueued)
                return;

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            _gameTime += dt;
            _totalTime += dt;

            TickTiltDecay();
            TickDeferredStops();
            TickScheduledTurnB();
            TickReactionDeadline();
            TickPhase(dt);

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryPushVisualState(force: false);
            }
        }

        public void Handle(MotionAction action)
        {
            if (!_initialized || !_running || _paused)
                return;

            if (_finishQueued)
                return;

            switch (action)
            {
                case MotionAction.Shake:
                    OnShake();
                    break;

                case MotionAction.Down:
                    OnDown();
                    break;

                case MotionAction.Up:
                    OnUp();
                    break;

                case MotionAction.TiltLeft:
                    _tiltDir = -1;
                    _tiltHoldUntilT = _gameTime + TiltSignalHoldSeconds;
                    break;

                case MotionAction.TiltRight:
                    _tiltDir = +1;
                    _tiltHoldUntilT = _gameTime + TiltSignalHoldSeconds;
                    break;
            }

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                RequestVisualPush(force: true);
            }
        }

        public void OnRepeatRequested() { }

        private void StopTutorialOnboardingRoutine()
        {
            if (_tutorialOnboardingCo != null)
            {
                StopCoroutine(_tutorialOnboardingCo);
                _tutorialOnboardingCo = null;
            }
        }

        private System.Collections.IEnumerator TutorialOnboardingRoutine()
        {
            if (_uiAudio == null || _speech == null)
            {
                _tutorialOnboardingCo = null;
                yield break;
            }

            SpeakGameKey(KeyHintObjective);
            yield return WaitForSpeechToFinishStable(TutorialSpeechStableFalseSeconds);

            if (!_running || _finishQueued) { _tutorialOnboardingCo = null; yield break; }

            string controlsKey = ResolveControlsHintKey();
            SpeakGameKey(controlsKey);
            yield return WaitForSpeechToFinishStable(TutorialSpeechStableFalseSeconds);

            yield return WaitUnscaledSecondsWhileRunning(TutorialPostOnboardingDelaySeconds);

            _tutorialOnboardingCo = null;
        }

        private System.Collections.IEnumerator WaitUnscaledSecondsWhileRunning(float seconds)
        {
            float d = Mathf.Clamp(seconds, 0f, 10f);
            float t = 0f;

            while (t < d)
            {
                if (!_running) yield break;
                if (_finishQueued) yield break;

                if (_paused)
                {
                    yield return null;
                    continue;
                }

                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private System.Collections.IEnumerator WaitForSpeechToFinishStable(float stableFalseSeconds)
        {
            if (_speech == null)
                yield break;

            stableFalseSeconds = Mathf.Clamp(stableFalseSeconds, 0.05f, 2.0f);
            float stable = 0f;

            while (true)
            {
                if (!_running) yield break;
                if (_finishQueued) yield break;

                if (_paused)
                {
                    yield return null;
                    continue;
                }

                bool speaking = false;
                try { speaking = _speech.IsSpeaking; } catch { speaking = false; }

                if (speaking)
                {
                    stable = 0f;
                    yield return null;
                    continue;
                }

                stable += Time.unscaledDeltaTime;
                if (stable >= stableFalseSeconds)
                    yield break;

                yield return null;
            }
        }

        private void SpeakGameKey(string key, params object[] args)
        {
            if (_uiAudio == null) return;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => SpeakGameKeyAndWait(ctx, key, args),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private System.Collections.IEnumerator SpeakGameKeyAndWait(UiAudioContext ctx, string key, params object[] args)
        {
            if (ctx?.Handle == null || ctx.Handle.IsCancelled)
                yield break;

            string localized = null;

            try
            {
                if (_loc != null)
                {
                    localized = (args == null || args.Length == 0)
                        ? _loc.GetFromTable(GameTable, key)
                        : _loc.GetFromTable(GameTable, key, args);
                }
            }
            catch
            {
                localized = null;
            }

            if (string.IsNullOrWhiteSpace(localized))
                localized = key;

            yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", localized);
        }

        private string ResolveControlsHintKey()
        {
            ControlHintMode mode = ControlHintMode.Auto;

            try
            {
                if (_settings != null)
                    mode = _settings.Current.controlHintMode;
            }
            catch
            {
                mode = ControlHintMode.Auto;
            }

            if (mode == ControlHintMode.Auto)
            {
                try { mode = StartupDefaultsResolver.ResolvePlatformPreferredHintMode(); }
                catch { mode = ControlHintMode.KeyboardMouse; }
            }

            return mode == ControlHintMode.Touch ? KeyHintTouch : KeyHintKeyboard;
        }

        private bool IsInputIgnored()
        {
            return _phase == Phase.Reeling && _gameTime < _ignoreInputUntilT;
        }

        private void OnShake()
        {
            switch (_phase)
            {
                case Phase.Idle:
                    TryPlayOneShotNoPan(SfxRodCast);
                    StartRound();
                    break;

                case Phase.Waiting:
                    TryPlayOneShotNoPan(SfxRodPullOut);
                    EnterIdle();
                    break;

                case Phase.Biting:
                    if (_biteIsOn)
                    {
                        RestartActionOneShot(SfxPull, useLinePan: false);
                        BeginReeling(fromBite: true);
                    }
                    else
                    {
                        TryPlayOneShotNoPan(SfxRodPullOut);
                        EnterIdle();
                    }
                    break;

                case Phase.Reeling:
                    if (CanCatchNow())
                    {
                        TryPlayOneShotNoPan(SfxRodPullOut);
                        BeginCatch();
                        return;
                    }

                    RestartActionOneShot(SfxPull, useLinePan: false);

                    if (IsInputIgnored())
                        return;

                    if (_fishAction == FishAction.Burst)
                    {
                        StopMoveAndTurnSfx();
                        StopBurstSounds();
                        _stopBurstLoopAtT = 0f;

                        _pendingTurnBPlays = 0;
                        _nextTurnBAtT = 0f;
                        _afterTurnLockUntilT = -999f;

                        RegisterCorrectAction();
                        ApplyIterativeCatchStep();
                        UpdateSplashingLoop();
                        TickHaptics();

                        BeginPostBurstPullGap();
                        RequestVisualPush(force: true);
                        return;
                    }

                    RegisterWrongActionAndResetToIdle();
                    return;
            }
        }

        private void BeginPostBurstPullGap()
        {
            _fishAction = FishAction.Idle;

            _idleReelsInCurrentIdle = 0;

            _moveDir = 0;
            _moveStartAtT = -999f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            _idleSfxPlayedThisIdle = false;

            float diff01 = ResolveDifficulty01();
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds) * Mathf.Lerp(1.35f, 0.95f, diff01);

            _reactionDeadlineAtT = 0f;

            float until = _gameTime + Mathf.Max(0.05f, PostBurstPullGapSeconds);
            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, until);
            _suppressIdleSfxUntilT = Mathf.Max(_suppressIdleSfxUntilT, until);
            _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, until);

            ResolveFishAudioBase(out float nowVol01, out float _);
            float before = Mathf.Clamp01(_preBurstFishVolume01);
            float after = Mathf.Max(0.01f, Mathf.Clamp01(nowVol01));
            float mul = Mathf.Clamp(before / after, 1.0f, 1.85f);
            _postBurstFishVolumeMul = mul;
            _postBurstFishVolumeMulUntilT = _gameTime + Mathf.Max(0.05f, PostBurstLoudnessBlendSeconds);
        }

        private void OnDown()
        {
            if (_phase == Phase.Biting)
                return;

            if (_phase != Phase.Reeling)
                return;

            RestartActionOneShot(SfxReel, useLinePan: true);

            if (IsInputIgnored())
                return;

            if (_fishAction == FishAction.Burst)
            {
                RegisterWrongActionAndResetToIdle();
                return;
            }

            if (_fishAction == FishAction.Move)
            {
                if (!IsTiltActiveOrForgiven())
                {
                    RegisterWrongActionAndResetToIdle();
                    return;
                }

                int tilt = Mathf.Clamp(_tiltDir, -1, 1);
                if (tilt == 0)
                {
                    RegisterWrongActionAndResetToIdle();
                    return;
                }

                if (tilt == _moveDir)
                {
                    RegisterWrongActionAndResetToIdle();
                    return;
                }

                StopMoveAndTurnSfx();
                SnapCenterFromMove();

                RegisterCorrectAction();
                ApplyIterativeCatchStep();
                if (CanCatchNow())
                    UpdateSplashingLoop();
                return;
            }

            if (_idleReelsInCurrentIdle >= MaxIdleReelsPerIdle)
            {
                RegisterWrongActionAndResetToIdle();
                return;
            }

            _idleReelsInCurrentIdle++;

            RegisterCorrectAction();
            ApplyIterativeCatchStep();
            if (CanCatchNow())
                UpdateSplashingLoop();

            if (_idleReelsInCurrentIdle >= MaxIdleReelsPerIdle)
            {
                ForceNonIdleAfterIdleTrap();
                return;
            }
        }

        private void OnUp()
        {
            if (_phase == Phase.Biting)
                return;

            if (_phase != Phase.Reeling)
                return;

            RestartActionOneShot(SfxLoosen, useLinePan: true);

            if (IsInputIgnored())
                return;

            _tensionTicks = 0;

            int before = _pullsToCatchProgress;
            int after = Mathf.FloorToInt(before * 0.5f);
            if (before > 0 && after == before) after = Mathf.Max(0, before - 1);
            _pullsToCatchProgress = Mathf.Clamp(after, 0, Mathf.Max(0, _pullsToCatchTarget));

            UpdateDistanceFromProgress(withSlack: true);

            float diff01 = ResolveDifficulty01();
            float a = Mathf.Clamp01(_aggression);
            float penalty = Mathf.Lerp(_loosenDistancePenaltyMin, _loosenDistancePenaltyMax, diff01) * Mathf.Lerp(0.90f, 1.10f, a);
            _fishDistance = Mathf.Min(BoardForwardMax, _fishDistance + penalty);

            _fatigue = Mathf.Clamp01(_fatigue - _fatigueLossOnLoosen);

            UpdateSplashingLoop();
            TickHaptics();
        }

        private void TickDeferredStops()
        {
            if (_stopHookBiteAtT > 0f && _gameTime >= _stopHookBiteAtT)
            {
                _stopHookBiteAtT = 0f;
                StopLoop(ref _hookBiteLoop);
            }

            if (_stopBurstLoopAtT > 0f && _gameTime >= _stopBurstLoopAtT)
            {
                _stopBurstLoopAtT = 0f;
                StopBurstSounds();
            }

            if (_postBurstFishVolumeMulUntilT > 0f && _gameTime >= _postBurstFishVolumeMulUntilT)
            {
                _postBurstFishVolumeMulUntilT = 0f;
                _postBurstFishVolumeMul = 1.0f;
            }
        }

        private void TickScheduledTurnB()
        {
            if (_pendingTurnBPlays <= 0)
                return;

            if (_gameTime < _nextTurnBAtT)
                return;

            _pendingTurnBPlays--;

            PlayTurnBSegment(_turnPan);

            float segDur = Mathf.Max(0.01f, TurnBEndSeconds - TurnBStartSeconds);
            _reactionDeadlineAtT = Mathf.Max(_reactionDeadlineAtT, _gameTime + segDur + PostCueSafetyGapSeconds);

            if (_pendingTurnBPlays > 0)
            {
                _nextTurnBAtT = _gameTime + segDur + TurnDoubleGapSeconds;
            }
        }

        private void TickReactionDeadline()
        {
            if (_phase != Phase.Reeling)
                return;

            if (_reactionDeadlineAtT <= 0f)
                return;

            if (_gameTime < _reactionDeadlineAtT)
                return;

            _reactionDeadlineAtT = 0f;

            RegisterTimeoutAndResetToIdle();
        }

        private void TickPhase(float dt)
        {
            switch (_phase)
            {
                case Phase.Idle:
                    break;

                case Phase.Waiting:
                    if (_gameTime >= _nextBiteAtT)
                        EnterBiting();
                    break;

                case Phase.Biting:
                    TickBitingPulse();
                    break;

                case Phase.Reeling:
                    TickReeling(dt);
                    break;

                case Phase.RoundEnd:
                    break;

                case Phase.Win:
                    break;
            }
        }

        private void TickBitingPulse()
        {
            if (_gameTime < _biteSwitchAtT)
                return;

            if (_biteIsOn)
            {
                _biteIsOn = false;
                StopLoop(ref _hookBiteLoop);
                _stopHookBiteAtT = 0f;

                float off = UnityEngine.Random.Range(BiteOffMinSeconds, BiteOffMaxSeconds);
                _biteSwitchAtT = _gameTime + Mathf.Clamp(off, 0.10f, 2.0f);
            }
            else
            {
                _biteIsOn = true;
                StartLoopFishSimple(ref _hookBiteLoop, SfxHookBite);

                float baseOn = Mathf.Max(0.12f, _reactionWindowBaseSeconds * _reactionWindowScale);
                float on = baseOn * UnityEngine.Random.Range(BiteOnRandMinMul, BiteOnRandMaxMul);
                on = Mathf.Clamp(on, 0.12f, 3.5f);
                _biteSwitchAtT = _gameTime + on;
            }

            MarkVisualDirty();
        }

        private void EnterIdle()
        {
            _phase = Phase.Idle;
            _phaseStartAtT = _gameTime;

            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            StopActionOneShot();
            StopLineBreakOneShot();

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _tiltDir = 0;
            _tiltHoldUntilT = -999f;

            _fishDistance = 0f;
            _fishLateral = 0f;

            _tensionTicks = 0;
            _fatigue = 0f;

            _pullsToCatchTarget = 0;
            _pullsToCatchProgress = 0;
            _progressStartDistance = 0f;
            _idleReelsInCurrentIdle = 0;

            _fishAction = FishAction.Idle;
            _nextActionResampleAtT = -999f;

            _moveStartAtT = -999f;
            _moveDir = 0;

            _idleSfxPlayedThisIdle = false;
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds);

            _nextFishSfxCheckAtT = _gameTime + 999f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            _afterTurnLockUntilT = -999f;

            _reactionDeadlineAtT = 0f;
            _ignoreInputUntilT = 0f;

            _biteIsOn = false;
            _biteSwitchAtT = 0f;

            _suppressIdleSfxUntilT = 0f;

            _preBurstFishVolume01 = 1.0f;
            _postBurstFishVolumeMul = 1.0f;
            _postBurstFishVolumeMulUntilT = 0f;

            _idleSfxEndsAtT = 0f;
            _moveSfxEndsAtT = 0f;

            RequestVisualPush(force: true);
        }

        private void StartRound()
        {
            SpawnFish();

            _phase = Phase.Waiting;
            _phaseStartAtT = _gameTime;

            float diff01 = ResolveDifficulty01();
            float waitMul = Mathf.Lerp(0.85f, 0.55f, diff01);
            float wait = UnityEngine.Random.Range(_biteWaitMin, _biteWaitMax) * waitMul;
            wait = Mathf.Clamp(wait, 0.25f, 6.00f);
            _nextBiteAtT = _gameTime + Mathf.Max(0.01f, wait);

            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            StopActionOneShot();
            StopLineBreakOneShot();

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _idleSfxPlayedThisIdle = false;
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds);

            _nextFishSfxCheckAtT = _gameTime + 999f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            _afterTurnLockUntilT = -999f;

            _reactionDeadlineAtT = 0f;
            _ignoreInputUntilT = 0f;

            _biteIsOn = false;
            _biteSwitchAtT = 0f;

            _suppressIdleSfxUntilT = 0f;

            _preBurstFishVolume01 = 1.0f;
            _postBurstFishVolumeMul = 1.0f;
            _postBurstFishVolumeMulUntilT = 0f;

            _idleSfxEndsAtT = 0f;
            _moveSfxEndsAtT = 0f;

            RequestVisualPush(force: true);
        }

        private void EnterBiting()
        {
            _phase = Phase.Biting;
            _phaseStartAtT = _gameTime;

            _biteIsOn = true;
            StartLoopFishSimple(ref _hookBiteLoop, SfxHookBite);

            float baseOn = Mathf.Max(0.12f, _reactionWindowBaseSeconds * _reactionWindowScale);
            float on = baseOn * UnityEngine.Random.Range(BiteOnRandMinMul, BiteOnRandMaxMul);
            on = Mathf.Clamp(on, 0.12f, 3.5f);
            _biteSwitchAtT = _gameTime + on;

            _reactionDeadlineAtT = 0f;
            _ignoreInputUntilT = 0f;
            StopContinuousHaptics();

            _suppressIdleSfxUntilT = 0f;

            RequestVisualPush(force: true);
        }

        private void BeginReeling(bool fromBite)
        {
            _phase = Phase.Reeling;
            _phaseStartAtT = _gameTime;

            if (fromBite)
            {
                if (_hookBiteLoop != null)
                    _stopHookBiteAtT = _gameTime + HookBiteStopDelaySeconds;
                else
                    _stopHookBiteAtT = 0f;
            }

            _tensionTicks = 0;
            _maxTension = Mathf.Max(_maxTension, ResolveTension01());

            _idleSfxPlayedThisIdle = false;
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds);

            _reactionDeadlineAtT = 0f;
            _ignoreInputUntilT = 0f;

            _suppressIdleSfxUntilT = 0f;

            _nextActionResampleAtT = _gameTime;
            EnterFishActionIdle(forceCue: true);

            UpdateSplashingLoop();
            TickHaptics();

            _biteIsOn = false;
            _biteSwitchAtT = 0f;

            RequestVisualPush(force: true);
        }

        private void BeginCatch()
        {
            if (_phase != Phase.Reeling)
                return;

            _caught++;

            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            PlayCatchOneShot();

            TryVisualPulseCatch();
            RequestVisualPush(force: true);

            if (_caught >= _targetFishCount)
            {
                BeginWin();
                return;
            }

            BeginRoundEnd(delaySeconds: PostCatchDelaySeconds);
        }

        private void BeginWin()
        {
            if (_finishQueued)
                return;

            _finishQueued = true;
            _phase = Phase.Win;

            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            StopActionOneShot();
            StopLineBreakOneShot();

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            RequestVisualPush(force: true);

            StopFinishRoutineIfAny();
            _finishCo = StartCoroutine(WinRoutine());
        }

        private System.Collections.IEnumerator WinRoutine()
        {
            float t0 = 0f;
            while (t0 < Mathf.Max(0f, PreWinSoundDelaySeconds))
            {
                if (!_running) { _finishCo = null; yield break; }
                if (_paused) { yield return null; continue; }
                t0 += Time.unscaledDeltaTime;
                yield return null;
            }

            TryPlayOneShotNoPan(SfxWin);

            float t1 = 0f;
            while (t1 < Mathf.Max(0f, PostWinSoundDelaySeconds))
            {
                if (!_running) { _finishCo = null; yield break; }
                if (_paused) { yield return null; continue; }
                t1 += Time.unscaledDeltaTime;
                yield return null;
            }

            float t2 = 0f;
            float d2 = Mathf.Max(0.25f, PostWinExitDelaySeconds);

            while (t2 < d2)
            {
                if (!_running) { _finishCo = null; yield break; }
                if (_paused) { yield return null; continue; }
                t2 += Time.unscaledDeltaTime;
                yield return null;
            }

            FinishCompleted();
            _finishCo = null;
        }

        private void BeginRoundEnd(float delaySeconds)
        {
            _phase = Phase.RoundEnd;

            RequestVisualPush(force: true);

            StopFinishRoutineIfAny();
            _finishCo = StartCoroutine(RoundEndRoutine(delaySeconds));
        }

        private System.Collections.IEnumerator RoundEndRoutine(float delaySeconds)
        {
            float t = 0f;
            float d = Mathf.Max(0.05f, delaySeconds);

            while (t < d)
            {
                if (!_running) { _finishCo = null; yield break; }
                if (_paused) { yield return null; continue; }
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            EnterIdle();
            _finishCo = null;
        }

        private void StopFinishRoutineIfAny()
        {
            if (_finishCo != null)
            {
                StopCoroutine(_finishCo);
                _finishCo = null;
            }
        }

        private void TickReeling(float dt)
        {
            if (_gameTime >= _nextActionResampleAtT && _fishAction == FishAction.Idle)
                ResampleFishAction();

            TickFishMotion(dt);

            float t01 = ResolveTension01();
            if (t01 > _maxTension)
                _maxTension = t01;

            if (_tensionTicks >= _tensionMaxTicks)
            {
                LoseFishByTension();
                return;
            }

            TickFishSfx();
            TickSpatialAudio();

            UpdateSplashingLoop();
            TickHaptics();

            MarkVisualDirty();
        }

        private void TickFishMotion(float dt)
        {
            if (_fishAction == FishAction.Idle)
            {
                _fishLateral = Mathf.Lerp(_fishLateral, 0f, 1.35f * dt);
                return;
            }

            if (_fishAction == FishAction.Move)
            {
                if (_gameTime < _moveStartAtT)
                    return;

                float s = Mathf.Max(0.01f, _moveSpeed);
                _fishLateral += _moveDir * s * dt;
                _fishLateral = Mathf.Clamp(_fishLateral, -BoardSideHalf, BoardSideHalf);
                return;
            }

            if (_fishAction == FishAction.Burst)
            {
                float s = Mathf.Max(0.01f, _burstSpeed);
                _fishDistance = Mathf.Min(BoardForwardMax, _fishDistance + s * dt);
                return;
            }
        }

        private void ResampleFishAction()
        {
            var prev = _fishAction;

            float a = Mathf.Clamp01(_aggression);
            float diff01 = ResolveDifficulty01();

            float horizon = UnityEngine.Random.Range(_actionMinSeconds, _actionMaxSeconds) * Mathf.Lerp(1.35f, 0.95f, diff01);
            _nextActionResampleAtT = _gameTime + Mathf.Max(0.35f, horizon);

            float wIdle = Mathf.Lerp(0.86f, 0.55f, Mathf.Clamp01(0.90f * a + 0.75f * diff01));
            float wMove = Mathf.Lerp(0.11f, 0.30f, Mathf.Clamp01(0.90f * a + 0.75f * diff01));
            float wBurst = Mathf.Lerp(0.03f, 0.15f, Mathf.Clamp01(0.95f * a + 0.85f * diff01));

            if (prev == FishAction.Idle)
            {
                if (_idleReelsInCurrentIdle <= 0) { }
                else if (_idleReelsInCurrentIdle == 1) wIdle *= 0.75f;
                else if (_idleReelsInCurrentIdle == 2) wIdle *= 0.40f;
                else wIdle = 0f;
            }

            if (_fatigue >= 0.75f)
                wIdle += 0.10f;

            if (_tensionTicks >= 2)
                wIdle += 0.12f;

            bool isMediumOrHard = diff01 > 0.40f;
            bool isHard = diff01 > 0.75f;

            bool highAgg = a >= 0.72f;
            bool midAgg = a >= 0.55f;

            if (!isMediumOrHard)
            {
                if (prev == FishAction.Move)
                    wBurst = 0f;

                if (prev == FishAction.Burst)
                {
                    wMove = 0f;
                    wBurst = 0f;
                }
            }
            else
            {
                if (prev == FishAction.Move)
                {
                    if (!highAgg)
                        wBurst = 0f;
                    else
                        wBurst *= isHard ? Mathf.Lerp(0.30f, 1.00f, a) : 0.12f;
                }

                if (prev == FishAction.Burst)
                {
                    if (!midAgg)
                        wMove = 0f;
                    else
                        wMove *= isHard ? Mathf.Lerp(0.40f, 1.00f, a) : 0.18f;

                    if (!highAgg)
                        wBurst = 0f;
                    else
                        wBurst *= isHard ? Mathf.Lerp(0.25f, 1.00f, a) : 0.10f;
                }
            }

            float sum = Mathf.Max(0.0001f, wIdle + wMove + wBurst);
            float r = UnityEngine.Random.value * sum;

            if (r < wIdle)
            {
                EnterFishActionIdle(forceCue: true);
                RequestVisualPush(force: true);
                return;
            }

            r -= wIdle;

            if (r < wMove)
            {
                EnterFishActionMove();
                RequestVisualPush(force: true);
                return;
            }

            EnterFishActionBurst();
            RequestVisualPush(force: true);
        }

        private bool IsTurnSequenceActive()
        {
            return _phase == Phase.Reeling && _fishAction == FishAction.Move && (_pendingTurnBPlays > 0 || _gameTime < _afterTurnLockUntilT);
        }

        private void EnterFishActionIdle(bool forceCue)
        {
            _fishAction = FishAction.Idle;

            _idleReelsInCurrentIdle = 0;

            _moveDir = 0;
            _moveStartAtT = -999f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _idleSfxPlayedThisIdle = false;

            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, _gameTime + MinIdleHoldSeconds);

            float diff01 = ResolveDifficulty01();
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds) * Mathf.Lerp(1.35f, 0.95f, diff01);

            if (forceCue)
                _nextFishSfxCheckAtT = Mathf.Min(_nextFishSfxCheckAtT, _gameTime + 0.18f);

            _afterTurnLockUntilT = -999f;

            _reactionDeadlineAtT = 0f;
        }

        private void EnterFishActionMove()
        {
            _fishAction = FishAction.Move;

            _idleReelsInCurrentIdle = 0;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopFishMoveOneShot();

            float diff01 = ResolveDifficulty01();
            float a = Mathf.Clamp01(_aggression);

            int dir = UnityEngine.Random.value < 0.5f ? -1 : +1;
            _moveDir = dir;

            float s = Mathf.Lerp(_moveLateralSpeedMin, _moveLateralSpeedMax, diff01) * Mathf.Lerp(0.92f, 1.15f, a);
            _moveSpeed = s;

            _idleSfxPlayedThisIdle = true;

            _turnPan = _moveDir < 0 ? -1f : 1f;

            _pendingTurnBPlays = 2;
            _nextTurnBAtT = _gameTime;
            TickScheduledTurnB();

            float totalTurn = Mathf.Max(0.01f, TurnSequenceTotalSeconds);

            _afterTurnLockUntilT = _gameTime + totalTurn + AfterDoubleTurnToMoveGapSeconds;
            _moveStartAtT = _afterTurnLockUntilT;

            _nextFishSfxCheckAtT = Mathf.Max(_afterTurnLockUntilT + 0.10f, _gameTime + 0.60f);

            float moveLen = Mathf.Max(0.35f, 0.90f + 0.35f * diff01);
            _moveSfxCueIndex = 0;

            float t0 = Mathf.Max(_moveStartAtT + 0.06f, _afterTurnLockUntilT);

            bool twoCues = diff01 <= 0.40f;

            if (twoCues)
            {
                _moveSfxCue1AtT = t0 + moveLen * 0.33f;
                _moveSfxCue2AtT = t0 + moveLen * 0.66f;
            }
            else
            {
                _moveSfxCue1AtT = t0 + moveLen * 0.55f;
                _moveSfxCue2AtT = 999f;
            }

            float minWindow = Mathf.Max(MinMoveReactionWindowSeconds, 0.35f + 0.25f * diff01);
            _reactionDeadlineAtT = _moveStartAtT + minWindow;
        }

        private void EnterFishActionBurst()
        {
            _fishAction = FishAction.Burst;

            _idleReelsInCurrentIdle = 0;

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            float diff01 = ResolveDifficulty01();
            float a = Mathf.Clamp01(_aggression);

            _burstSpeed = Mathf.Lerp(_burstForwardSpeedMin, _burstForwardSpeedMax, diff01) * Mathf.Lerp(0.95f, 1.18f, a);

            _idleSfxPlayedThisIdle = true;

            _nextFishSfxCheckAtT = _gameTime + 999f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            ResolveFishAudioBase(out float vol01, out float _);
            _preBurstFishVolume01 = Mathf.Clamp01(vol01);

            StartBurstSounds();

            float diffMin = Mathf.Lerp(2.05f, 1.75f, diff01);
            float minWindow = Mathf.Max(MinBurstReactionWindowSeconds, diffMin);
            _reactionDeadlineAtT = _gameTime + minWindow;

            _stopBurstLoopAtT = Mathf.Max(_gameTime + 0.20f, _reactionDeadlineAtT - BurstStopBeforeDeadlineSeconds);
        }

        private void ForceNonIdleAfterIdleTrap()
        {
            float diff01 = ResolveDifficulty01();
            float a = Mathf.Clamp01(_aggression);

            float wMove = Mathf.Lerp(0.65f, 0.55f, diff01) * Mathf.Lerp(1.05f, 0.95f, a);

            float r = UnityEngine.Random.value;
            if (r < Mathf.Clamp01(wMove))
                EnterFishActionMove();
            else
                EnterFishActionBurst();

            RequestVisualPush(force: true);
        }

        private void PlayTurnBSegment(float pan)
        {
            if (_audioFx == null) return;

            StopTurnBOneShot();

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = 1.0f;
            opt.PanStereo = pan < 0f ? -1f : 1f;
            opt.Pitch = 1.0f;
            opt.StartTimeSeconds = TurnBStartSeconds;
            opt.EndTimeSeconds = TurnBEndSeconds;

            try { _turnBOneShot = _audioFx.PlayCurrentGameSoundControlled(SfxTurn, opt); }
            catch { _turnBOneShot = null; }
        }

        private float ResolveFishPanOnly()
        {
            float x01 = 0f;
            if (BoardSideHalf > 0.0001f)
                x01 = Mathf.Clamp(_fishLateral / BoardSideHalf, -1f, 1f);
            return x01;
        }

        private void StartBurstSounds()
        {
            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            if (_audioFx == null) return;

            float pan = ResolveFishPanOnly();
            float vol01 = 1.0f;

            var loopOpt = AudioFxPlayOptions.Default;
            loopOpt.Loop = true;
            loopOpt.Volume01 = Mathf.Clamp01(vol01 * BurstVolumeBoost);
            loopOpt.PanStereo = Mathf.Clamp(pan, -1f, 1f);
            loopOpt.Pitch = 1.0f;
            loopOpt.StartTimeSeconds = BurstLoopStart;
            loopOpt.EndTimeSeconds = BurstLoopEnd;

            try { _burstLoop = _audioFx.PlayCurrentGameSoundControlled(SfxBurst, loopOpt); }
            catch { _burstLoop = null; }
        }

        private void StopBurstSounds()
        {
            if (_burstLoop != null) { try { _burstLoop.Stop(); } catch { } _burstLoop = null; }
        }

        private void TickFishSfx()
        {
            if (_gameTime < _phaseStartAtT + 0.25f)
                return;

            if (_gameTime < _nextFishSfxCheckAtT)
                return;

            if (IsTurnSequenceActive())
            {
                _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _afterTurnLockUntilT + 0.10f);
                return;
            }

            float diff01 = ResolveDifficulty01();
            ResolveFishSfxGaps(diff01, out float baseGapMin, out float baseGapMax);

            if (_fishAction == FishAction.Idle)
            {
                if (_gameTime < _suppressIdleSfxUntilT)
                {
                    _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _suppressIdleSfxUntilT);
                    return;
                }

                if (CanCatchNow())
                {
                    _nextFishSfxCheckAtT = _gameTime + 999f;
                    return;
                }

                if (!_idleSfxPlayedThisIdle)
                {
                    PlayIdleCueOneShot(forcePan: 0f);
                    _idleSfxPlayedThisIdle = true;

                    _nextFishSfxCheckAtT = Mathf.Min(_nextIdleReminderAtT, _gameTime + UnityEngine.Random.Range(baseGapMin, baseGapMax));
                    _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + AfterFishCueMinGap);
                    return;
                }

                if (_gameTime >= _nextIdleReminderAtT)
                {
                    PlayIdleCueOneShot(forcePan: 0f);

                    _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds) * Mathf.Lerp(1.30f, 0.95f, diff01);

                    _nextFishSfxCheckAtT = _gameTime + UnityEngine.Random.Range(baseGapMin, baseGapMax);
                    _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + AfterFishCueMinGap);
                    return;
                }

                _nextFishSfxCheckAtT = Mathf.Min(_nextIdleReminderAtT, _gameTime + UnityEngine.Random.Range(baseGapMin, baseGapMax));
                _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + 0.35f);
                return;
            }

            if (_fishAction == FishAction.Move)
            {
                if (_gameTime < _moveStartAtT)
                {
                    _nextFishSfxCheckAtT = Mathf.Max(_moveStartAtT + 0.10f, _afterTurnLockUntilT + 0.10f);
                    return;
                }

                if (_moveSfxCueIndex == 0 && _gameTime >= _moveSfxCue1AtT)
                {
                    float pan = _turnPan;
                    PlayMoveCueOneShot(forcePan: pan);
                    _moveSfxCueIndex = 1;

                    _reactionDeadlineAtT = Mathf.Max(_reactionDeadlineAtT, _moveSfxEndsAtT + PostCueSafetyGapSeconds);

                    _nextFishSfxCheckAtT = _gameTime + UnityEngine.Random.Range(baseGapMin, baseGapMax) * 0.85f;
                    _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + 0.55f);
                    return;
                }

                if (_moveSfxCue2AtT < 999f && _moveSfxCueIndex == 1 && _gameTime >= _moveSfxCue2AtT)
                {
                    float pan = _turnPan;
                    PlayMoveCueOneShot(forcePan: pan);
                    _moveSfxCueIndex = 2;

                    _reactionDeadlineAtT = Mathf.Max(_reactionDeadlineAtT, _moveSfxEndsAtT + PostCueSafetyGapSeconds);

                    _nextFishSfxCheckAtT = _gameTime + UnityEngine.Random.Range(baseGapMin, baseGapMax);
                    _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + 0.65f);
                    return;
                }

                _nextFishSfxCheckAtT = _gameTime + 0.25f;
                return;
            }

            _nextFishSfxCheckAtT = _gameTime + 999f;
        }

        private static void ResolveFishSfxGaps(float diff01, out float minGap, out float maxGap)
        {
            if (diff01 <= 0.40f)
            {
                minGap = MinFishCueGapEasy;
                maxGap = MaxFishCueGapEasy;
                return;
            }

            if (diff01 <= 0.75f)
            {
                minGap = MinFishCueGapMedium;
                maxGap = MaxFishCueGapMedium;
                return;
            }

            minGap = MinFishCueGapHard;
            maxGap = MaxFishCueGapHard;
        }

        private void SnapCenterFromMove()
        {
            float before = _fishLateral;
            float target = Mathf.Lerp(before, 0f, CenterSnapStrength);
            _fishLateral = target;

            if (Mathf.Abs(_fishLateral) <= 0.06f)
                _fishLateral = 0f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            EnterFishActionIdle(forceCue: true);

            MarkVisualDirty();
        }

        private int ResolveMistakeStepBackCount()
        {
            string mode = _ctx.ModeId ?? string.Empty;

            if (string.Equals(mode, "tutorial", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(mode, "easy", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(mode, "medium", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (string.Equals(mode, "hard", StringComparison.OrdinalIgnoreCase))
                return 3;

            float diff01 = ResolveDifficulty01();
            if (diff01 <= 0.40f) return 1;
            if (diff01 <= 0.75f) return 2;
            return 3;
        }

        private void ApplyStepBackOnMistake()
        {
            if (_pullsToCatchTarget <= 0)
                return;

            int stepBack = Mathf.Clamp(ResolveMistakeStepBackCount(), 1, 6);
            _pullsToCatchProgress = Mathf.Clamp(_pullsToCatchProgress - stepBack, 0, Mathf.Max(0, _pullsToCatchTarget));

            UpdateDistanceFromProgress(withSlack: true);

            if (CanCatchNow())
                UpdateSplashingLoop();
        }

        private void RegisterWrongActionAndResetToIdle()
        {
            TryVisualPulseMistakeBlink();

            if (_tensionTicks < _tensionMaxTicks)
                _tensionTicks++;

            ApplyStepBackOnMistake();

            _fatigue = Mathf.Clamp01(_fatigue - _fatigueLossOnWrong);

            if (ResolveTension01() > _maxTension)
                _maxTension = ResolveTension01();

            if (!IsContinuousHapticsActive() && _tensionTicks < _tensionMaxTicks)
                TryPulseErrorOnce();

            if (_tensionTicks >= _tensionMaxTicks)
            {
                LoseFishByTension();
                return;
            }

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, _gameTime + MinIdleHoldSeconds);
            _suppressIdleSfxUntilT = Mathf.Max(_suppressIdleSfxUntilT, _gameTime + MinIdleHoldSeconds);
            EnterFishActionIdle(forceCue: true);

            _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + 0.30f);

            TickHaptics();

            RequestVisualPush(force: true);
        }

        private void RegisterTimeoutAndResetToIdle()
        {
            TryVisualPulseMistakeBlink();

            _lostTimeout++;

            if (_tensionTicks < _tensionMaxTicks)
                _tensionTicks++;

            ApplyStepBackOnMistake();

            _fatigue = Mathf.Clamp01(_fatigue - _fatigueLossOnWrong);

            if (ResolveTension01() > _maxTension)
                _maxTension = ResolveTension01();

            if (!IsContinuousHapticsActive() && _tensionTicks < _tensionMaxTicks)
                TryPulseErrorOnce();

            if (_tensionTicks >= _tensionMaxTicks)
            {
                LoseFishByTension();
                return;
            }

            _ignoreInputUntilT = _gameTime + _failGraceSeconds;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopMoveAndTurnSfx();

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, _gameTime + MinIdleHoldSeconds);
            _suppressIdleSfxUntilT = Mathf.Max(_suppressIdleSfxUntilT, _gameTime + MinIdleHoldSeconds);
            EnterFishActionIdle(forceCue: true);

            _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _gameTime + 0.30f);

            TickHaptics();

            RequestVisualPush(force: true);
        }

        private void RegisterCorrectAction()
        {
            _fatigue = Mathf.Clamp01(_fatigue + _fatigueGainOnCorrect);
        }

        private bool IsContinuousHapticsActive()
        {
            return _hapticsContinuous != null;
        }

        private void TryPulseErrorOnce()
        {
            if (_haptics == null) return;
            try { _haptics.Pulse(HapticLevel.Medium); } catch { }
        }

        private bool CanCatchNow()
        {
            return _fishDistance <= _catchDistance;
        }

        private void UpdateSplashingLoop()
        {
            if (CanCatchNow())
                StartLoopFishSimple(ref _splashLoop, SfxSplashing);
            else
                StopLoop(ref _splashLoop);
        }

        private void LoseFishByTension()
        {
            _lostLine++;

            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            StopMoveAndTurnSfx();

            RestartLineBreakOneShot();

            RequestVisualPush(force: true);

            BeginRoundEnd(delaySeconds: PostRoundResetDelaySeconds);
        }

        private void SpawnFish()
        {
            _aggression = UnityEngine.Random.Range(_aggressionMin, _aggressionMax);
            _resistance = UnityEngine.Random.Range(_resistanceMin, _resistanceMax);

            float jitter = UnityEngine.Random.Range(-_spawnDistanceJitter, +_spawnDistanceJitter);
            _fishDistance = _spawnDistanceBase + jitter + _difficultySpawnBias;
            _fishDistance = Mathf.Clamp(_fishDistance, 0.05f, 0.95f);

            _progressStartDistance = _fishDistance;

            _fishLateral = 0f;

            _tensionTicks = 0;
            _fatigue = 0f;

            _catchDistance = _catchDistanceBase / Mathf.Max(0.10f, _resistance);

            ResolvePullsToCatchTarget(out int min, out int max);
            float mod = Mathf.Lerp(0.92f, 1.12f, Mathf.Clamp01(0.65f * _aggression + 0.10f * ResolveDifficulty01()));
            int baseTarget = UnityEngine.Random.Range(min, max + 1);
            _pullsToCatchTarget = Mathf.Clamp(Mathf.RoundToInt(baseTarget * mod), 3, 18);
            _pullsToCatchProgress = 0;

            _idleReelsInCurrentIdle = 0;

            _fishAction = FishAction.Idle;
            _nextActionResampleAtT = _gameTime;

            _idleSfxPlayedThisIdle = false;
            _nextIdleReminderAtT = _gameTime + UnityEngine.Random.Range(MinIdleReminderSeconds, MaxIdleReminderSeconds);

            _nextFishSfxCheckAtT = _gameTime + 0.55f;

            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            _afterTurnLockUntilT = -999f;

            _reactionDeadlineAtT = 0f;
            _ignoreInputUntilT = 0f;

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _biteIsOn = false;
            _biteSwitchAtT = 0f;

            _suppressIdleSfxUntilT = 0f;

            _preBurstFishVolume01 = 1.0f;
            _postBurstFishVolumeMul = 1.0f;
            _postBurstFishVolumeMulUntilT = 0f;

            _idleSfxEndsAtT = 0f;
            _moveSfxEndsAtT = 0f;

            RequestVisualPush(force: true);
        }

        private void ResolvePullsToCatchTarget(out int min, out int max)
        {
            string mode = _ctx.ModeId ?? string.Empty;

            if (string.Equals(mode, "tutorial", StringComparison.OrdinalIgnoreCase))
            {
                min = 3; max = 5;
                return;
            }

            if (string.Equals(mode, "easy", StringComparison.OrdinalIgnoreCase))
            {
                min = 4; max = 7;
                return;
            }

            if (string.Equals(mode, "medium", StringComparison.OrdinalIgnoreCase))
            {
                min = 7; max = 10;
                return;
            }

            if (string.Equals(mode, "hard", StringComparison.OrdinalIgnoreCase))
            {
                min = 10; max = 15;
                return;
            }

            float diff01 = ResolveDifficulty01();
            if (diff01 <= 0.40f) { min = 4; max = 7; return; }
            if (diff01 <= 0.75f) { min = 7; max = 10; return; }
            min = 10; max = 15;
        }

        private float ComputeProgressSlack()
        {
            float diff01 = ResolveDifficulty01();
            float a = Mathf.Clamp01(_aggression);
            float baseSlack = Mathf.Lerp(0.08f, 0.04f, diff01);
            float mod = Mathf.Lerp(1.10f, 0.90f, a);
            return Mathf.Clamp(baseSlack * mod, 0.025f, 0.10f);
        }

        private float ComputeProgressDistanceSuggested()
        {
            if (_pullsToCatchTarget <= 0)
                return Mathf.Clamp(_fishDistance, 0f, BoardForwardMax);

            float t = _pullsToCatchProgress / Mathf.Max(1f, (float)_pullsToCatchTarget);
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            float start = Mathf.Clamp(_progressStartDistance, 0f, BoardForwardMax);
            float target = Mathf.Clamp(_catchDistance, 0f, BoardForwardMax);

            return Mathf.Lerp(start, target, eased);
        }

        private void ApplyIterativeCatchStep()
        {
            if (_pullsToCatchTarget <= 0)
                return;

            _pullsToCatchProgress = Mathf.Min(_pullsToCatchTarget, _pullsToCatchProgress + 1);

            UpdateDistanceFromProgress(withSlack: true);

            if (_pullsToCatchProgress >= _pullsToCatchTarget)
                _fishDistance = Mathf.Min(_fishDistance, _catchDistance);

            MarkVisualDirty();
        }

        private void UpdateDistanceFromProgress(bool withSlack)
        {
            float suggested = ComputeProgressDistanceSuggested();
            float slack = withSlack ? ComputeProgressSlack() : 0f;

            float minAllowed = Mathf.Clamp(suggested, 0f, BoardForwardMax);
            float maxAllowed = Mathf.Clamp(suggested + slack, 0f, BoardForwardMax);

            _fishDistance = Mathf.Clamp(_fishDistance, minAllowed, maxAllowed);

            if (_fishDistance < 0f)
                _fishDistance = 0f;
        }

        private void TickTiltDecay()
        {
            if (_gameTime > _tiltHoldUntilT)
                _tiltDir = 0;
        }

        private bool IsTiltActiveOrForgiven()
        {
            if (_tiltDir == 0) return false;
            return _gameTime <= (_tiltHoldUntilT + TiltForgivenessSeconds);
        }

        private float ResolveTension01()
        {
            float denom = Mathf.Max(1, _tensionMaxTicks);
            return Mathf.Clamp01(_tensionTicks / denom);
        }

        private void TickHaptics()
        {
            if (_haptics == null)
                return;

            if (_tensionTicks >= _tensionHapticsTick)
            {
                if (_hapticsContinuous == null)
                {
                    try { _hapticsContinuous = _haptics.StartContinuous(HapticLevel.Light); } catch { _hapticsContinuous = null; }
                }
                return;
            }

            StopContinuousHaptics();
        }

        private void StopContinuousHaptics()
        {
            if (_hapticsContinuous != null)
            {
                try { _hapticsContinuous.Stop(); } catch { }
                _hapticsContinuous = null;
            }
        }

        private void TickSpatialAudio()
        {
            UpdateLoopSpatialFish(_hookBiteLoop, isBurstLoop: false);
            UpdateLoopSpatialFish(_splashLoop, isBurstLoop: false);

            if (_burstLoop != null && _burstLoop.IsValid)
                UpdateLoopSpatialFish(_burstLoop, isBurstLoop: true);
        }

        private void FinishCompleted()
        {
            StopAllHandlesAndClear();

            var result = new GameplayGameResult(
                reason: GameplayGameFinishReason.Completed,
                score: 0,
                runtimeStats: GetRuntimeStatsSnapshot()
            );

            SyncVisualModeState(forcePush: false);

            if (_visualAssistActive)
            {
                EnsureVisualDriverResolvedIfNeeded();
                TryVisualSetVisible(false);
                RequestVisualPush(force: true);
            }
            else
            {
                ReleaseVisualDriverIfAny();
            }

            try { GameFinished?.Invoke(result); } catch { }
        }

        public IReadOnlyDictionary<string, string> GetRuntimeStatsSnapshot()
        {
            return new Dictionary<string, string>(64)
            {
                ["fishing.modeId"] = _ctx.ModeId,

                ["fishing.targetFishCount"] = _targetFishCount.ToString(CultureInfo.InvariantCulture),
                ["fishing.caught"] = _caught.ToString(CultureInfo.InvariantCulture),

                ["fishing.lostTimeout"] = _lostTimeout.ToString(CultureInfo.InvariantCulture),
                ["fishing.lostLineBreak"] = _lostLine.ToString(CultureInfo.InvariantCulture),

                ["fishing.maxTension"] = _maxTension.ToString("0.###", CultureInfo.InvariantCulture),
                ["fishing.totalTime"] = _totalTime.ToString("0.###", CultureInfo.InvariantCulture),

                ["fishing.difficultyScale"] = _difficultyScale.ToString("0.###", CultureInfo.InvariantCulture),

                ["fishing.tensionMaxTicks"] = _tensionMaxTicks.ToString(CultureInfo.InvariantCulture),
                ["fishing.failGraceSeconds"] = _failGraceSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            };
        }

        private void ResetRuntimeState(bool fullReset)
        {
            _gameTime = 0f;

            _caught = 0;
            _lostTimeout = 0;
            _lostLine = 0;
            _maxTension = 0f;
            _totalTime = 0f;

            if (fullReset)
            {
                _phase = Phase.Idle;
                _phaseStartAtT = 0f;

                _nextBiteAtT = 0f;

                _fishDistance = 0f;
                _fishLateral = 0f;

                _aggression = 0f;
                _resistance = 1f;

                _tensionTicks = 0;
                _fatigue = 0f;

                _catchDistance = _catchDistanceBase;

                _fishAction = FishAction.Idle;
                _nextActionResampleAtT = 0f;
                _moveStartAtT = -999f;
                _moveDir = 0;
                _moveSpeed = 0f;
                _burstSpeed = 0f;

                _idleSfxPlayedThisIdle = false;
                _nextIdleReminderAtT = 0f;

                _tiltHoldUntilT = -999f;
                _tiltDir = 0;

                _nextFishSfxCheckAtT = 0f;

                _moveSfxCue1AtT = 999f;
                _moveSfxCue2AtT = 999f;
                _moveSfxCueIndex = 0;

                _afterTurnLockUntilT = -999f;

                _stopHookBiteAtT = 0f;

                _stopBurstLoopAtT = 0f;

                _reactionDeadlineAtT = 0f;
                _ignoreInputUntilT = 0f;

                _pullsToCatchTarget = 0;
                _pullsToCatchProgress = 0;
                _progressStartDistance = 0f;

                _idleReelsInCurrentIdle = 0;

                _pendingTurnBPlays = 0;
                _nextTurnBAtT = 0f;

                _biteIsOn = false;
                _biteSwitchAtT = 0f;

                _suppressIdleSfxUntilT = 0f;

                _preBurstFishVolume01 = 1.0f;
                _postBurstFishVolumeMul = 1.0f;
                _postBurstFishVolumeMulUntilT = 0f;

                _idleSfxEndsAtT = 0f;
                _moveSfxEndsAtT = 0f;

                _actionOneShotSoundId = null;
                _actionOneShotStartedAtT = 0f;

                StopContinuousHaptics();
            }
        }

        private void TryPlayOneShotNoPan(string soundId)
        {
            if (_audioFx == null) return;
            try { _audioFx.PlayCurrentGameSound(soundId); } catch { }
        }

        private void PlayIdleCueOneShot(float forcePan)
        {
            if (_audioFx == null) return;

            if (_idleSfxEndsAtT > 0f && _gameTime < _idleSfxEndsAtT)
                return;

            ResolveFishAudio(out float vol01, out float _);

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = Mathf.Clamp01(vol01);
            opt.PanStereo = Mathf.Clamp(forcePan, -1f, 1f);
            opt.Pitch = 1.0f;
            opt.StartTimeSeconds = FishCueTrimLeadSeconds;
            opt.EndTimeSeconds = 0f;

            try { _idleCueOneShot = _audioFx.PlayCurrentGameSoundControlled(SfxIdle, opt); }
            catch { _idleCueOneShot = null; }

            _idleSfxEndsAtT = _gameTime + Mathf.Max(0.01f, IdleCueClipSeconds);

            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, _idleSfxEndsAtT + AfterFishCueMinGap);
            _nextActionResampleAtT = Mathf.Max(_nextActionResampleAtT, _idleSfxEndsAtT + PostCueSafetyGapSeconds);

            _nextFishSfxCheckAtT = Mathf.Max(_nextFishSfxCheckAtT, _idleSfxEndsAtT + AfterFishCueMinGap);

            MarkVisualDirty();
        }

        private void PlayMoveCueOneShot(float forcePan)
        {
            if (_audioFx == null) return;

            StopFishMoveOneShot();

            ResolveFishAudio(out float vol01, out float _);

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = Mathf.Clamp01(vol01);
            opt.PanStereo = Mathf.Clamp(forcePan, -1f, 1f);
            opt.Pitch = 1.0f;
            opt.StartTimeSeconds = FishCueTrimLeadSeconds;
            opt.EndTimeSeconds = 0f;

            try { _fishMoveOneShot = _audioFx.PlayCurrentGameSoundControlled(SfxMove, opt); }
            catch { _fishMoveOneShot = null; }

            _moveSfxEndsAtT = _gameTime + Mathf.Max(0.01f, MoveCueClipSeconds);

            _reactionDeadlineAtT = Mathf.Max(_reactionDeadlineAtT, _moveSfxEndsAtT + PostCueSafetyGapSeconds);

            MarkVisualDirty();
        }

        private void PlayCatchOneShot()
        {
            if (_audioFx == null) return;

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = 1.0f;
            opt.PanStereo = 0f;
            opt.Pitch = 1.0f;

            try { _catchOneShot = _audioFx.PlayCurrentGameSoundControlled(SfxCatch, opt); }
            catch { _catchOneShot = null; }
        }

        private void RestartActionOneShot(string soundId, bool useLinePan)
        {
            if (_audioFx == null) return;

            float pan = 0f;
            if (useLinePan)
                pan = ResolveLinePanFull();

            if (_actionOneShot != null && _actionOneShot.IsValid &&
                string.Equals(_actionOneShotSoundId, soundId, StringComparison.Ordinal) &&
                (_gameTime - _actionOneShotStartedAtT) >= 0f &&
                (_gameTime - _actionOneShotStartedAtT) < ActionOneShotMinRestartGapSeconds)
            {
                try { _actionOneShot.SetPan(Mathf.Clamp(pan, -1f, 1f)); } catch { }
                return;
            }

            StopActionOneShot();

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = 1.0f;
            opt.PanStereo = Mathf.Clamp(pan, -1f, 1f);
            opt.Pitch = 1.0f;

            try
            {
                _actionOneShot = _audioFx.PlayCurrentGameSoundControlled(soundId, opt);
                _actionOneShotSoundId = soundId;
                _actionOneShotStartedAtT = _gameTime;
            }
            catch
            {
                _actionOneShot = null;
                _actionOneShotSoundId = null;
                _actionOneShotStartedAtT = 0f;
            }
        }

        private void StopActionOneShot()
        {
            if (_actionOneShot != null) { try { _actionOneShot.Stop(); } catch { } _actionOneShot = null; }
            _actionOneShotSoundId = null;
            _actionOneShotStartedAtT = 0f;
        }

        private void RestartLineBreakOneShot()
        {
            if (_audioFx == null) return;

            StopLineBreakOneShot();

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = false;
            opt.Volume01 = 1.0f;
            opt.PanStereo = 0f;
            opt.Pitch = 1.0f;

            try { _lineBreakOneShot = _audioFx.PlayCurrentGameSoundControlled(SfxLineBreak, opt); }
            catch { _lineBreakOneShot = null; }
        }

        private void StopLineBreakOneShot()
        {
            if (_lineBreakOneShot != null) { try { _lineBreakOneShot.Stop(); } catch { } _lineBreakOneShot = null; }
        }

        private void StartLoopFishSimple(ref AudioFxHandle h, string soundId)
        {
            if (_audioFx == null) return;
            if (h != null) return;

            ResolveFishAudio(out float vol01, out float pan);

            var opt = AudioFxPlayOptions.Default;
            opt.Loop = true;
            opt.Volume01 = Mathf.Clamp01(vol01);
            opt.PanStereo = Mathf.Clamp(pan, -1f, 1f);
            opt.Pitch = 1.0f;

            try { h = _audioFx.PlayCurrentGameSoundControlled(soundId, opt); }
            catch { h = null; }
        }

        private void UpdateLoopSpatialFish(AudioFxHandle h, bool isBurstLoop)
        {
            if (h == null || !h.IsValid) return;

            if (isBurstLoop)
            {
                float pan = ResolveFishPanOnly();
                float v = Mathf.Clamp01(1.0f * BurstVolumeBoost);
                try { h.SetVolume01(v); } catch { }
                try { h.SetPan(pan); } catch { }
                try { h.SetPitch(1.0f); } catch { }
                return;
            }

            ResolveFishAudio(out float vol01, out float panNormal);

            try { h.SetVolume01(vol01); } catch { }
            try { h.SetPan(panNormal); } catch { }
            try { h.SetPitch(1.0f); } catch { }
        }

        private void StopLoop(ref AudioFxHandle h)
        {
            if (h == null) return;
            try { h.Stop(); } catch { }
            h = null;
        }

        private void StopFishMoveOneShot()
        {
            if (_fishMoveOneShot != null) { try { _fishMoveOneShot.Stop(); } catch { } _fishMoveOneShot = null; }
        }

        private void StopTurnBOneShot()
        {
            if (_turnBOneShot != null) { try { _turnBOneShot.Stop(); } catch { } _turnBOneShot = null; }
        }

        private void StopMoveAndTurnSfx()
        {
            StopFishMoveOneShot();
            StopTurnBOneShot();
            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;
            _afterTurnLockUntilT = -999f;
            _moveSfxCue1AtT = 999f;
            _moveSfxCue2AtT = 999f;
            _moveSfxCueIndex = 0;

            _moveSfxEndsAtT = 0f;
        }

        private void PauseKnownHandles()
        {
            try { _hookBiteLoop?.Pause(); } catch { }
            try { _burstLoop?.Pause(); } catch { }
            try { _splashLoop?.Pause(); } catch { }
            try { _actionOneShot?.Pause(); } catch { }
            try { _lineBreakOneShot?.Pause(); } catch { }
            try { _fishMoveOneShot?.Pause(); } catch { }
            try { _turnBOneShot?.Pause(); } catch { }
            try { _idleCueOneShot?.Pause(); } catch { }
            try { _catchOneShot?.Pause(); } catch { }

            StopContinuousHaptics();
        }

        private void ResumeKnownHandles()
        {
            try { _hookBiteLoop?.Resume(); } catch { }
            try { _burstLoop?.Resume(); } catch { }
            try { _splashLoop?.Resume(); } catch { }
            try { _actionOneShot?.Resume(); } catch { }
            try { _lineBreakOneShot?.Resume(); } catch { }
            try { _fishMoveOneShot?.Resume(); } catch { }
            try { _turnBOneShot?.Resume(); } catch { }
            try { _idleCueOneShot?.Resume(); } catch { }
            try { _catchOneShot?.Resume(); } catch { }

            _hapticsContinuous = null;
        }

        private void StopAllHandlesAndClear()
        {
            StopLoop(ref _hookBiteLoop);
            _stopHookBiteAtT = 0f;

            StopBurstSounds();
            _stopBurstLoopAtT = 0f;

            StopLoop(ref _splashLoop);

            StopContinuousHaptics();

            StopActionOneShot();
            StopLineBreakOneShot();

            StopMoveAndTurnSfx();

            if (_catchOneShot != null) { try { _catchOneShot.Stop(); } catch { } _catchOneShot = null; }

            if (_idleCueOneShot != null) { try { _idleCueOneShot.Stop(); } catch { } _idleCueOneShot = null; }

            _pendingTurnBPlays = 0;
            _nextTurnBAtT = 0f;

            _postBurstFishVolumeMul = 1.0f;
            _postBurstFishVolumeMulUntilT = 0f;

            _idleSfxEndsAtT = 0f;
            _moveSfxEndsAtT = 0f;
        }

        private void ResolveServices()
        {
            var services = Core.App.AppContext.Services;

            try { _audioFx = services.Resolve<IAudioFxService>(); } catch { _audioFx = null; }
            try { _haptics = services.Resolve<IHapticsService>(); } catch { _haptics = null; }
            try { _visualMode = services.Resolve<IVisualModeService>(); } catch { _visualMode = null; }

            try { _uiAudio = services.Resolve<IUiAudioOrchestrator>(); } catch { _uiAudio = null; }
            try { _loc = services.Resolve<ILocalizationService>(); } catch { _loc = null; }
            try { _settings = services.Resolve<ISettingsService>(); } catch { _settings = null; }
            try { _speech = services.Resolve<ISpeechService>(); } catch { _speech = null; }
        }

        private void ResolveVisual()
        {
            _visual = null;

            if (!_visualAssistActive)
                return;

            try
            {
                var services = Core.App.AppContext.Services;
                try { _visual = services.Resolve<IFishingVisualDriver>(); } catch { _visual = null; }
            }
            catch { }

            if (_visual != null)
                return;

            try
            {
                var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(includeInactive: true);
                for (int i = 0; i < all.Length; i++)
                {
                    var mb = all[i];
                    if (mb == null) continue;
                    if (mb is IFishingVisualDriver v)
                    {
                        _visual = v;
                        break;
                    }
                }
            }
            catch { }
        }

        private void ResolveFishAudioBase(out float volume01, out float pan)
        {
            float x01 = 0f;
            if (BoardSideHalf > 0.0001f)
                x01 = Mathf.Clamp(_fishLateral / BoardSideHalf, -1f, 1f);

            pan = x01;

            float d01 = Mathf.Clamp01(_fishDistance / BoardForwardMax);
            volume01 = Mathf.Clamp01(Mathf.Lerp(1.0f, FishAudioFarVolume, d01));
        }

        private void ResolveFishAudio(out float volume01, out float pan)
        {
            ResolveFishAudioBase(out float v, out pan);

            if (_postBurstFishVolumeMulUntilT > 0f && _gameTime < _postBurstFishVolumeMulUntilT)
            {
                float t01 = Mathf.Clamp01(1f - ((_postBurstFishVolumeMulUntilT - _gameTime) / Mathf.Max(0.01f, PostBurstLoudnessBlendSeconds)));
                float mul = Mathf.Lerp(_postBurstFishVolumeMul, 1.0f, t01);
                v = Mathf.Clamp01(v * Mathf.Clamp(mul, 1.0f, 1.85f));
            }

            volume01 = v;
        }

        private float ResolveLinePanFull()
        {
            if (IsTiltActiveOrForgiven())
                return Mathf.Clamp(_tiltDir, -1, 1) * LinePanMax;

            return 0f;
        }

        private float ResolveDifficulty01()
        {
            float d = Mathf.Clamp(_difficultyScale, 0.10f, 10f);
            float t = Mathf.InverseLerp(0.50f, 1.35f, d);
            return Mathf.Clamp01(t);
        }

        private static float GetRequiredFloat(IReadOnlyDictionary<string, string> dict, string key, float min, float max)
        {
            if (!dict.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
                throw new InvalidOperationException($"FishingGame: missing required parameter '{key}'.");

            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"FishingGame: invalid float for '{key}': '{s}'.");

            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new InvalidOperationException($"FishingGame: invalid float (NaN/Inf) for '{key}'.");

            if (v < min || v > max)
                throw new InvalidOperationException($"FishingGame: parameter '{key}' out of range [{min}..{max}] value={v.ToString(CultureInfo.InvariantCulture)}.");

            return v;
        }

        private static int GetRequiredInt(IReadOnlyDictionary<string, string> dict, string key, int min, int max)
        {
            if (!dict.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
                throw new InvalidOperationException($"FishingGame: missing required parameter '{key}'.");

            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"FishingGame: invalid int for '{key}': '{s}'.");

            if (v < min || v > max)
                throw new InvalidOperationException($"FishingGame: parameter '{key}' out of range [{min}..{max}] value={v.ToString(CultureInfo.InvariantCulture)}.");

            return v;
        }

        private FishingVisualPhase MapPhase(Phase p)
        {
            switch (p)
            {
                case Phase.Idle: return FishingVisualPhase.Idle;
                case Phase.Waiting: return FishingVisualPhase.Waiting;
                case Phase.Biting: return FishingVisualPhase.Biting;
                case Phase.Reeling: return FishingVisualPhase.Reeling;
                case Phase.RoundEnd: return FishingVisualPhase.RoundEnd;
                case Phase.Win: return FishingVisualPhase.Win;
                default: return FishingVisualPhase.Idle;
            }
        }

        private FishingVisualFishAction MapAction(FishAction a)
        {
            switch (a)
            {
                case FishAction.Idle: return FishingVisualFishAction.Idle;
                case FishAction.Move: return FishingVisualFishAction.Move;
                case FishAction.Burst: return FishingVisualFishAction.Burst;
                default: return FishingVisualFishAction.Idle;
            }
        }

        private void TryPushVisualState(bool force)
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;

            bool floatVisible =
                _running &&
                !_finishQueued &&
                (_phase == Phase.Waiting || _phase == Phase.Biting || _phase == Phase.Reeling);

            float dist01 = Mathf.Clamp01(_fishDistance / Mathf.Max(0.0001f, BoardForwardMax));

            float lat01 = 0f;
            if (BoardSideHalf > 0.0001f)
                lat01 = Mathf.Clamp(_fishLateral / BoardSideHalf, -1f, 1f);

            bool biteOn = (_phase == Phase.Biting) && _biteIsOn;

            var s = new FishingVisualState(
                visible: _running && !_finishQueued,
                paused: _paused,
                phase: MapPhase(_phase),
                fishAction: MapAction(_fishAction),
                floatVisible: floatVisible,
                fishDistance01: dist01,
                fishLateral01: lat01,
                biteIsOn: biteOn,
                canCatchNow: CanCatchNow(),
                tensionTicks: Mathf.Clamp(_tensionTicks, 0, Mathf.Max(0, _tensionMaxTicks)),
                tensionMaxTicks: Mathf.Max(0, _tensionMaxTicks),
                tensionWarnTick: Mathf.Clamp(_tensionHapticsTick, 0, Mathf.Max(0, _tensionMaxTicks))
            );

            float interval = floatVisible ? VisualPushMinIntervalSeconds : VisualPushIdleIntervalSeconds;

            if (!force)
            {
                if (_gameTime < _nextVisualPushAtT && !_visualDirty)
                    return;

                if (_hasLastPushedVisual)
                {
                    bool changed =
                        _lastPushedVisual.Visible != s.Visible ||
                        _lastPushedVisual.Paused != s.Paused ||
                        _lastPushedVisual.Phase != s.Phase ||
                        _lastPushedVisual.FishAction != s.FishAction ||
                        _lastPushedVisual.FloatVisible != s.FloatVisible ||
                        _lastPushedVisual.BiteIsOn != s.BiteIsOn ||
                        _lastPushedVisual.CanCatchNow != s.CanCatchNow ||
                        _lastPushedVisual.TensionTicks != s.TensionTicks ||
                        _lastPushedVisual.TensionMaxTicks != s.TensionMaxTicks ||
                        _lastPushedVisual.TensionWarnTick != s.TensionWarnTick ||
                        Mathf.Abs(_lastPushedVisual.FishDistance01 - s.FishDistance01) >= VisualDistanceEpsilon ||
                        Mathf.Abs(_lastPushedVisual.FishLateral01 - s.FishLateral01) >= VisualLateralEpsilon;

                    if (!changed && !_visualDirty && _gameTime < _nextVisualPushAtT)
                        return;

                    if (!changed && !_visualDirty)
                    {
                        _nextVisualPushAtT = _gameTime + interval;
                        return;
                    }
                }
            }

            try { _visual.Apply(in s); } catch { }

            _lastPushedVisual = s;
            _hasLastPushedVisual = true;
            _visualDirty = false;
            _nextVisualPushAtT = _gameTime + interval;
        }

        private void TryVisualReset()
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;
            try { _visual.Reset(); } catch { }
        }

        private void TryVisualSetVisible(bool visible)
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;
            try { _visual.SetVisible(visible); } catch { }
        }

        private void TryVisualSetPaused(bool paused)
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;
            try { _visual.SetPaused(paused); } catch { }
        }

        private void TryVisualPulseMistakeBlink()
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;
            try { _visual.PulseMistakeBlink(); } catch { }
        }

        private void TryVisualPulseCatch()
        {
            if (!_visualAssistActive) return;
            if (_visual == null) return;
            try { _visual.PulseCatch(); } catch { }
        }
    }
}