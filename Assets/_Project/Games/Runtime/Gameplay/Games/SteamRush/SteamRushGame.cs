using Project.Core.App;
using Project.Core.AudioFx;
using Project.Core.Haptics;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Visual;
using Project.Games.Gameplay.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Project.Games.SteamRush
{
    public sealed class SteamRushGame : MonoBehaviour,
        IGameplayGame,
        IGameplayInputHandler,
        IGameplayRuntimeStatsProvider,
        IGameplayRepeatHandler
    {
        private const string GameId = "steamrush";
        private const string GameTable = "Game_SteamRush";

        private const int LaneCount = 3;
        private const int DefaultLane = 1;

        private const HapticLevel HapticOutOfBounds = HapticLevel.Medium;
        private const HapticLevel HapticCollision = HapticLevel.Strong;

        private const float MinApproachSecondsClamp = 1.05f;

        private const float MinCollisionWindowSeconds = 0.25f;
        private const float MaxCollisionWindowSeconds = 1.10f;

        private const float MinEmitGapSecondsHardClamp = 0.16f;

        private const float CollisionExitDelaySeconds = 1.55f;
        private const float WinExitDelaySeconds = 1.55f;

        private const float PanAbsNear = 0.94f;
        private const float PanFarBoost = 1.25f;

        private const float DuckSeconds = 0.25f;
        private const float DuckOtherMul = 0.55f;

        private const float VolSameLane = 1.00f;
        private const float VolAdjacentLane = 0.62f;
        private const float VolFarLane = 0.34f;

        private const float BedBudget01 = 0.92f;
        private const float PlayerLaneBedAttenuation = 0.78f;

        private const float CrossfadeFracOfCollision = 0.45f;
        private const float CrossfadeMinSeconds = 0.08f;
        private const float CrossfadeMaxSeconds = 0.16f;

        private const float GainEdgeMain = 0.80f;
        private const float GainEdgeBleed = 0.10f;
        private const float GainCenter = 0.64f;

        private const float PitchCenterMul = 0.97f;
        private const float PitchSideDelta = 0.04f;
        private const float PitchFarExtra = 0.03f;

        private const float SwipePanAbs = 0.92f;

        private const float CenterBoostMul = 1.10f;
        private const float WhistleMul = 1.35f;

        private const string SfxSwishLeft = "SwishLeft";
        private const string SfxSwishRight = "SwishRight";
        private const string SfxTrainIntro = "TrainIntro";
        private const string SfxTrainLoop = "TrainLoop";
        private const string SfxWhistle = "Whistle";
        private const string SfxTrainPass = "TrainPass";
        private const string SfxWin = "Win";

        private const string PApproachSeconds = "steamrush.approachSeconds";
        private const string PPassSeconds = "steamrush.passSeconds";
        private const string PSpawnRateScale = "steamrush.spawnRateScale";

        private const string PDifficultyScale = "steamrush.difficultyScale";
        private const string PPatternTierMax = "steamrush.patternTierMax";
        private const string PModeDurationSeconds = "steamrush.modeDurationSeconds";
        private const string PWhistleMoment = "steamrush.whistleMoment";

        private const string EndlessRunKey0 = "steamrush.endless.run0";
        private const string EndlessRunKey1 = "steamrush.endless.run1";
        private const string EndlessRunKey2 = "steamrush.endless.run2";
        private const string EndlessRunKey3 = "steamrush.endless.run3";
        private const string EndlessRunKey4 = "steamrush.endless.run4";

        private IAudioFxService _audioFx;
        private IHapticsService _haptics;
        private IVisualModeService _visualMode;
        private ILocalizationService _loc;
        private ISettingsService _settings;

        private bool _initialized;
        private bool _running;
        private bool _paused;
        private bool _finishQueued;

        private string _modeId = "";

        private float _difficultyScale;
        private int _patternTierMax;
        private float _whistleMoment01;

        private float _baseApproachSeconds;
        private float _basePassSeconds;
        private float _baseSpawnRateScale;

        private float _approachSeconds;
        private float _collisionWindowSeconds;
        private float _spawnRateEffective;

        private float _shortSpacingBaseSeconds;

        private bool _hasWinCondition;
        private float _durationSeconds;

        private float _gameTime;
        private float _nextEmitTime;

        private float _prepDelaySeconds;
        private float _endlessPrepAdaptMul;
        private float _endlessVirtualDurationSeconds;

        private int _currentLane = DefaultLane;

        private int _laneMoves;
        private int _outOfBoundsHits;
        private int _trainsSpawned;
        private int _trainsPassed;
        private int _collisions;
        private int _maxConcurrentObserved;

        private float _duckUntilGameTime;

        private Coroutine _collisionCo;
        private Coroutine _winCo;

        // --- spawn budget (token bucket) ---
        private float _spawnTokens;
        private float _spawnTokenLastT;

        private struct Train
        {
            public int lane;

            public float startT;
            public float whistleT;
            public float impactT;
            public float endT;

            public bool introPlayed;
            public bool whistlePlayed;

            public bool impactResolved;
            public bool passStarted;

            public bool loopStarted;

            public float crossfadeStartT;
            public float crossfadeEndT;

            public AudioFxHandle introHandle;
            public AudioFxHandle loopHandle;
            public AudioFxHandle whistleHandle;
            public AudioFxHandle passHandle;
        }

        private readonly List<Train> _trains = new List<Train>(64);
        private readonly List<AudioFxHandle> _handles = new List<AudioFxHandle>(128);

        private enum BedKind { None, Intro, Loop, Pass }
        private readonly List<int> _audible = new List<int>(8);
        private readonly List<float> _bedE = new List<float>(8);
        private readonly List<BedKind> _bedK = new List<BedKind>(8);

        public event Action<GameplayGameResult> GameFinished;

        private enum PatternKind
        {
            Single,
            ExpressSingle,

            ParallelDouble,

            StairsInternal,
            GapStairs,
            SwapSingle,
            ExpressDouble,

            ZigzagDense,
            StairsExternal,
            MultiSwap
        }

        private struct PatternDef
        {
            public PatternKind kind;
            public int tier;
            public bool dense;
            public int minCount;
            public int maxCount;
            public float minSpacingFactor;
            public float maxSpacingFactor;
        }

        private static readonly PatternDef[] PatternLibrary = new PatternDef[]
        {
            new PatternDef { kind = PatternKind.Single, tier = 0, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 1f, maxSpacingFactor = 1f },
            new PatternDef { kind = PatternKind.ExpressSingle, tier = 0, dense = false, minCount = 2, maxCount = 4, minSpacingFactor = 1.00f, maxSpacingFactor = 1.00f },

            new PatternDef { kind = PatternKind.ParallelDouble, tier = 1, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 1f, maxSpacingFactor = 1f },

            new PatternDef { kind = PatternKind.StairsInternal, tier = 2, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 0.95f, maxSpacingFactor = 1.20f },
            new PatternDef { kind = PatternKind.GapStairs, tier = 2, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 0.95f, maxSpacingFactor = 1.20f },
            new PatternDef { kind = PatternKind.SwapSingle, tier = 2, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 0.95f, maxSpacingFactor = 1.20f },
            new PatternDef { kind = PatternKind.ExpressDouble, tier = 2, dense = false, minCount = 2, maxCount = 4, minSpacingFactor = 1.00f, maxSpacingFactor = 1.00f },

            new PatternDef { kind = PatternKind.ZigzagDense, tier = 2, dense = true, minCount = 1, maxCount = 1, minSpacingFactor = 0.75f, maxSpacingFactor = 0.95f },

            new PatternDef { kind = PatternKind.StairsExternal, tier = 3, dense = false, minCount = 1, maxCount = 1, minSpacingFactor = 0.95f, maxSpacingFactor = 1.20f },
            new PatternDef { kind = PatternKind.MultiSwap, tier = 3, dense = false, minCount = 2, maxCount = 4, minSpacingFactor = 0.90f, maxSpacingFactor = 1.10f },
        };

        public void Initialize(GameplayGameContext ctx)
        {
            ResolveServices();

            _modeId = NormalizeAndValidateModeId(ctx.ModeId);

            if (ctx.InitialParameters == null)
                throw new InvalidOperationException("SteamRushGame.Initialize: ctx.InitialParameters is null (required).");

            _baseApproachSeconds = GetRequiredFloat(ctx.InitialParameters, PApproachSeconds, min: 0.25f, max: 10.0f);
            _basePassSeconds = GetRequiredFloat(ctx.InitialParameters, PPassSeconds, min: 0.10f, max: 10.0f);
            _baseSpawnRateScale = GetRequiredFloat(ctx.InitialParameters, PSpawnRateScale, min: 0.05f, max: 10.0f);

            _difficultyScale = GetRequiredFloat(ctx.InitialParameters, PDifficultyScale, min: 0.10f, max: 10.0f);
            _patternTierMax = Mathf.Clamp(GetRequiredInt(ctx.InitialParameters, PPatternTierMax, min: 0, max: 3), 0, 3);

            _whistleMoment01 = GetRequiredFloat(ctx.InitialParameters, PWhistleMoment, min: 0.10f, max: 0.90f);

            if (IsEndlessMode(_modeId))
            {
                _hasWinCondition = false;
                _durationSeconds = 0f;
            }
            else
            {
                _hasWinCondition = true;
                _durationSeconds = GetRequiredFloat(ctx.InitialParameters, PModeDurationSeconds, min: 5f, max: 3600f);
            }

            float timeFactor = ResolveTimeFactorFromDifficulty(_difficultyScale);

            _approachSeconds = Mathf.Max(MinApproachSecondsClamp, _baseApproachSeconds * timeFactor);

            _collisionWindowSeconds = Mathf.Clamp(
                _basePassSeconds * timeFactor,
                MinCollisionWindowSeconds,
                MaxCollisionWindowSeconds
            );

            _spawnRateEffective = Mathf.Clamp(_baseSpawnRateScale * _difficultyScale, 0.05f, 10.0f);

            _shortSpacingBaseSeconds = Mathf.Clamp(_approachSeconds * 0.23f, 0.12f, 0.55f);

            _initialized = true;
            _running = false;
            _paused = false;
            _finishQueued = false;

            _currentLane = DefaultLane;

            _laneMoves = 0;
            _outOfBoundsHits = 0;
            _trainsSpawned = 0;
            _trainsPassed = 0;
            _collisions = 0;
            _maxConcurrentObserved = 0;

            _duckUntilGameTime = -999f;

            _gameTime = 0f;

            // token bucket init
            _spawnTokens = 0f;
            _spawnTokenLastT = 0f;

            ComputeEndlessAdaptationIfNeeded();
            _prepDelaySeconds = ComputePreparationDelaySeconds();
            _nextEmitTime = Mathf.Max(0.01f, _prepDelaySeconds);

            _trains.Clear();

            StopCollisionRoutineIfAny();
            StopWinRoutineIfAny();
            StopAllHandlesAndClear();
        }

        public void StartGame()
        {
            if (!_initialized) return;

            _running = true;
            _paused = false;

            ApplyVisualVisibility();
        }

        public void StopGame()
        {
            _running = false;
            _paused = false;

            StopCollisionRoutineIfAny();
            StopWinRoutineIfAny();

            _trains.Clear();
            StopAllHandlesAndClear();

            ApplyVisualVisibility(forceVisible: false);
        }

        public void PauseGame()
        {
            _paused = true;
            PauseAllHandles();
            ApplyVisualVisibility();
        }

        public void ResumeGame()
        {
            _paused = false;
            ResumeAllHandles();
            RefreshAllTrainAudioMix();
            ApplyVisualVisibility();
        }

        private void Update()
        {
            if (!_initialized || !_running || _paused)
                return;

            if (_finishQueued)
                return;

            _gameTime += Time.unscaledDeltaTime;

            PruneInvalidHandles();

            TickEmit(_gameTime);
            TickTrains(_gameTime);
            TickWinCondition(_gameTime);
        }

        public void Handle(NavAction action)
        {
            if (!_initialized || !_running || _paused)
                return;

            if (_finishQueued)
                return;

            switch (action)
            {
                case NavAction.Next:
                    TryMoveLane(+1);
                    break;

                case NavAction.Previous:
                    TryMoveLane(-1);
                    break;

                case NavAction.Confirm:
                    OnRepeatRequested();
                    break;
            }
        }

        private void TryMoveLane(int delta)
        {
            int target = _currentLane + delta;

            if (target < 0 || target >= LaneCount)
            {
                _outOfBoundsHits++;
                PulseHaptic(HapticOutOfBounds);
                return;
            }

            if (target == _currentLane)
                return;

            _currentLane = target;
            _laneMoves++;

            PlayMoveSfx(delta);

            TriggerWhistlesOnLaneChangeIfNeeded();
            RefreshAllTrainAudioMix();
        }

        public void OnRepeatRequested()
        {
        }

        private void TickEmit(float nowT)
        {
            if (nowT < _nextEmitTime)
                return;

            float elapsed = nowT;

            int maxConcurrent = ResolveMaxConcurrent(elapsed);
            int active = CountActive(nowT);

            // refill token bucket
            UpdateSpawnTokens(nowT, elapsed, maxConcurrent);

            // if board full -> don't hammer per-frame
            if (active >= maxConcurrent)
            {
                _nextEmitTime = nowT + Mathf.Max(0.10f, ComputeMinEmitIntervalSeconds(maxConcurrent) * 0.25f);
                return;
            }

            // if we can't afford even 1 train yet -> schedule next check when token arrives
            if (_spawnTokens < 0.999f)
            {
                float rate = Mathf.Max(0.05f, ResolveAllowedTrainsPerSecond(elapsed, maxConcurrent));
                float wait = Mathf.Clamp((1.0f - _spawnTokens) / rate, 0.05f, 0.50f);
                _nextEmitTime = nowT + wait;
                return;
            }

            int activeTierMax = ResolveActiveTierMax(elapsed);
            int phasePeakCap = ResolvePhasePeakCap(elapsed);

            int tokenBudget = Mathf.Clamp((int)_spawnTokens, 1, 32);

            // cap burst per emit: tutorial 1, easy 2, medium 3, hard/endless 4 (and never above maxConcurrent)
            int burstCap =
                IsTutorialMode(_modeId) ? 1 :
                IsEasyMode(_modeId) ? 2 :
                IsMediumMode(_modeId) ? 3 :
                4;

            burstCap = Mathf.Clamp(burstCap, 1, Mathf.Max(1, maxConcurrent));
            tokenBudget = Mathf.Min(tokenBudget, burstCap);

            PatternDef pat = ChoosePattern(activeTierMax, maxConcurrent, active, phasePeakCap, tokenBudget);

            float span = SchedulePattern(nowT, pat, tokenBudget, out int trainsScheduled);

            if (trainsScheduled > 0)
                _spawnTokens = Mathf.Max(0f, _spawnTokens - trainsScheduled);

            float gapBase = Mathf.Clamp(_approachSeconds * 0.33f, 0.18f, 1.50f);
            float gap = gapBase / Mathf.Max(0.001f, _spawnRateEffective);
            gap = Mathf.Max(MinEmitGapSecondsHardClamp, gap);

            float minInterval = ComputeMinEmitIntervalSeconds(maxConcurrent);

            _nextEmitTime = nowT + Mathf.Max(0.01f, Mathf.Max(minInterval, span + gap));
        }

        private void UpdateSpawnTokens(float nowT, float elapsed, int maxConcurrent)
        {
            float dt = nowT - _spawnTokenLastT;
            if (dt <= 0f) return;

            _spawnTokenLastT = nowT;

            float rate = ResolveAllowedTrainsPerSecond(elapsed, maxConcurrent);

            // cap burst size so "uzbierane" tokeny nie robią nagle młyna
            float burstCap =
                IsTutorialMode(_modeId) ? 1.0f :
                IsEasyMode(_modeId) ? 2.5f :
                IsMediumMode(_modeId) ? 3.5f :
                4.5f;

            burstCap = Mathf.Clamp(burstCap, 1f, 8f);

            _spawnTokens = Mathf.Min(burstCap, _spawnTokens + dt * rate);
        }

        private float ResolveAllowedTrainsPerSecond(float elapsed, int maxConcurrent)
        {
            float activeDur = Mathf.Max(0.20f, _approachSeconds + _collisionWindowSeconds);

            // "fizyczny" limit sensowny przy danym maxConcurrent:
            // jeśli chcesz mieć maxConcurrent aktywnych, to średnio maxConcurrent/activeDur startów/s ma sens.
            float phys = Mathf.Max(0.10f, maxConcurrent / activeDur);

            float p =
                IsEndlessMode(_modeId) ? ResolveEndlessProgress01(elapsed) :
                ResolveProgress01(elapsed);

            // ramp: na początku spokojniej, potem do celu
            float ramp = Mathf.Lerp(0.70f, 1.00f, Mathf.Clamp01(p));

            float modeMul =
                IsTutorialMode(_modeId) ? 0.55f :
                IsEasyMode(_modeId) ? 0.72f :
                IsMediumMode(_modeId) ? 0.85f :
                IsHardMode(_modeId) ? 0.95f :
                1.00f;

            // dodatkowa miękka korekta pod spawnRateEffective, ale bez pozwolenia na spam
            float sr = Mathf.Clamp(_spawnRateEffective, 0.10f, 10.0f);
            float srMul = Mathf.Lerp(0.90f, 1.05f, Mathf.InverseLerp(0.5f, 3.0f, sr));

            float v = phys * modeMul * ramp * srMul;

            // praktyczne clampy
            if (IsTutorialMode(_modeId)) v = Mathf.Min(v, 0.90f);
            if (IsEasyMode(_modeId)) v = Mathf.Min(v, 1.60f);
            if (IsMediumMode(_modeId)) v = Mathf.Min(v, 2.40f);
            if (IsHardMode(_modeId)) v = Mathf.Min(v, 3.20f);

            return Mathf.Clamp(v, 0.15f, 6.0f);
        }

        private float ComputeMinEmitIntervalSeconds(int maxConcurrent)
        {
            int c = Mathf.Max(1, maxConcurrent);
            float activeDur = Mathf.Max(0.05f, _approachSeconds + _collisionWindowSeconds);
            float baseInterval = activeDur / c;

            float mul =
                IsTutorialMode(_modeId) ? 1.25f :
                IsEasyMode(_modeId) ? 1.10f :
                IsMediumMode(_modeId) ? 1.02f :
                0.98f;

            float v = baseInterval * mul;
            return Mathf.Clamp(v, 0.08f, 10.0f);
        }

        private int ResolveActiveTierMax(float elapsed)
        {
            float t1 = _approachSeconds * 4.0f;
            float t2 = _approachSeconds * 8.0f;
            float t3 = _approachSeconds * 12.0f;

            int tier = 0;

            if (elapsed >= t1) tier = 1;
            if (elapsed >= t2) tier = 2;
            if (elapsed >= t3) tier = 3;

            tier = Mathf.Clamp(tier, 0, _patternTierMax);
            return tier;
        }

        private int ResolvePhasePeakCap(float elapsed)
        {
            if (IsTutorialMode(_modeId))
                return 1;

            if (IsEasyMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.45f) return 1;
                return 2;
            }

            if (IsMediumMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.33f) return 1;
                if (p < 0.66f) return 2;
                return 4;
            }

            if (IsHardMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.25f) return 2;
                if (p < 0.70f) return 3;
                return 4;
            }

            if (IsEndlessMode(_modeId))
            {
                float p = ResolveEndlessProgress01(elapsed);
                if (p < 0.25f) return 2;
                if (p < 0.60f) return 3;
                return 4;
            }

            return 2;
        }

        private PatternDef ChoosePattern(int activeTierMax, int maxConcurrent, int activeNow, int phasePeakCap, int tokenBudget)
        {
            if (IsTutorialMode(_modeId))
            {
                for (int i = 0; i < PatternLibrary.Length; i++)
                    if (PatternLibrary[i].kind == PatternKind.Single)
                        return PatternLibrary[i];

                throw new InvalidOperationException("SteamRush: tutorial Single pattern missing.");
            }

            Span<int> idx = stackalloc int[PatternLibrary.Length];
            int n = 0;

            int allowedNewPeakByBoard = Mathf.Max(0, maxConcurrent - activeNow);
            int allowedNewPeak = Mathf.Min(allowedNewPeakByBoard, phasePeakCap);

            for (int i = 0; i < PatternLibrary.Length; i++)
            {
                var p = PatternLibrary[i];

                if (p.tier > activeTierMax) continue;
                if (p.tier > _patternTierMax) continue;

                int peak = PatternPeakSimultaneous(p.kind);
                if (peak > allowedNewPeak) continue;

                int minTr = PatternMinTrains(p.kind, p.minCount);
                if (minTr > tokenBudget) continue;

                idx[n++] = i;
            }

            if (n <= 0)
            {
                for (int i = 0; i < PatternLibrary.Length; i++)
                {
                    var p = PatternLibrary[i];
                    if (p.kind == PatternKind.Single)
                        return p;
                }

                throw new InvalidOperationException("SteamRush: no patterns available for current constraints.");
            }

            int pick = idx[UnityEngine.Random.Range(0, n)];
            return PatternLibrary[pick];
        }

        private static int PatternMinTrains(PatternKind kind, int minCount)
        {
            minCount = Mathf.Max(1, minCount);

            return kind switch
            {
                PatternKind.Single => 1,
                PatternKind.ExpressSingle => minCount,             // 2..4
                PatternKind.ParallelDouble => 2,
                PatternKind.StairsInternal => 2,
                PatternKind.GapStairs => 2,
                PatternKind.StairsExternal => 2,
                PatternKind.SwapSingle => 4,
                PatternKind.ZigzagDense => 4,
                PatternKind.ExpressDouble => 2 * minCount,         // 4..8
                PatternKind.MultiSwap => 4 * minCount,             // 8..16
                _ => 1
            };
        }

        private static int PatternPeakSimultaneous(PatternKind kind)
        {
            return kind switch
            {
                PatternKind.Single => 1,
                PatternKind.ExpressSingle => 1,

                PatternKind.ParallelDouble => 2,
                PatternKind.ExpressDouble => 2,

                PatternKind.StairsInternal => 2,
                PatternKind.GapStairs => 2,
                PatternKind.StairsExternal => 2,

                PatternKind.SwapSingle => 4,
                PatternKind.ZigzagDense => 4,
                PatternKind.MultiSwap => 4,

                _ => 4
            };
        }

        private float SchedulePattern(float nowT, PatternDef pat, int tokenBudget, out int trainsScheduled)
        {
            trainsScheduled = 0;

            int countRand = UnityEngine.Random.Range(pat.minCount, pat.maxCount + 1);
            float spacingFactor = UnityEngine.Random.Range(pat.minSpacingFactor, pat.maxSpacingFactor);
            float dtShort = Mathf.Clamp(_shortSpacingBaseSeconds * spacingFactor, 0.08f, 1.00f);

            switch (pat.kind)
            {
                case PatternKind.Single:
                    {
                        if (tokenBudget < 1) return 0f;
                        int lane = RandomLaneAny();
                        ScheduleTrain(nowT, lane);
                        trainsScheduled = 1;
                        return 0f;
                    }

                case PatternKind.ExpressSingle:
                    {
                        int maxCountByBudget = Mathf.Max(1, tokenBudget);
                        int count = Mathf.Clamp(countRand, 1, maxCountByBudget);

                        // jeśli budżet jest mały, nie wymuszaj serii – lepiej 1
                        if (count <= 1)
                        {
                            int lane = RandomLaneAny();
                            ScheduleTrain(nowT, lane);
                            trainsScheduled = 1;
                            return 0f;
                        }

                        int lane2 = RandomLaneAny();
                        float t = nowT;
                        for (int i = 0; i < count; i++)
                        {
                            ScheduleTrain(t, lane2);
                            t += dtShort;
                        }

                        trainsScheduled = count;
                        return Mathf.Max(0f, (count - 1) * dtShort);
                    }

                case PatternKind.ParallelDouble:
                    {
                        if (tokenBudget < 2) goto case PatternKind.Single;

                        ChooseParallelDouble(out int a, out int b);
                        ScheduleTrain(nowT, a);
                        ScheduleTrain(nowT, b);
                        trainsScheduled = 2;
                        return 0f;
                    }

                case PatternKind.StairsInternal:
                    {
                        if (tokenBudget < 2) goto case PatternKind.Single;

                        bool left = UnityEngine.Random.value < 0.5f;
                        int edge = left ? 0 : 2;
                        ScheduleTrain(nowT, edge);
                        ScheduleTrain(nowT + dtShort, 1);
                        trainsScheduled = 2;
                        return dtShort;
                    }

                case PatternKind.GapStairs:
                    {
                        if (tokenBudget < 2) goto case PatternKind.Single;

                        bool leftToRight = UnityEngine.Random.value < 0.5f;
                        int a = leftToRight ? 0 : 2;
                        int b = leftToRight ? 2 : 0;
                        ScheduleTrain(nowT, a);
                        ScheduleTrain(nowT + dtShort, b);
                        trainsScheduled = 2;
                        return dtShort;
                    }

                case PatternKind.StairsExternal:
                    {
                        if (tokenBudget < 2) goto case PatternKind.Single;

                        bool toLeft = UnityEngine.Random.value < 0.5f;
                        int edge = toLeft ? 0 : 2;
                        ScheduleTrain(nowT, 1);
                        ScheduleTrain(nowT + dtShort, edge);
                        trainsScheduled = 2;
                        return dtShort;
                    }

                case PatternKind.SwapSingle:
                    {
                        if (tokenBudget < 4)
                        {
                            // degradacja: jak stać tylko na 2 -> schody, inaczej single
                            if (tokenBudget >= 2) goto case PatternKind.StairsInternal;
                            goto case PatternKind.Single;
                        }

                        bool lmToMr = UnityEngine.Random.value < 0.5f;

                        if (lmToMr)
                        {
                            ScheduleTrain(nowT, 0);
                            ScheduleTrain(nowT, 1);
                            ScheduleTrain(nowT + dtShort, 1);
                            ScheduleTrain(nowT + dtShort, 2);
                        }
                        else
                        {
                            ScheduleTrain(nowT, 1);
                            ScheduleTrain(nowT, 2);
                            ScheduleTrain(nowT + dtShort, 0);
                            ScheduleTrain(nowT + dtShort, 1);
                        }

                        trainsScheduled = 4;
                        return dtShort;
                    }

                case PatternKind.ZigzagDense:
                    {
                        if (tokenBudget < 4)
                        {
                            if (tokenBudget >= 2) goto case PatternKind.GapStairs;
                            goto case PatternKind.Single;
                        }

                        bool leftFirst = UnityEngine.Random.value < 0.5f;
                        float dtWithin = dtShort;
                        float dtBetween = Mathf.Clamp(dtShort * 0.85f, 0.06f, 0.80f);

                        if (leftFirst)
                        {
                            ScheduleTrain(nowT, 0);
                            ScheduleTrain(nowT + dtWithin, 1);

                            ScheduleTrain(nowT + dtBetween, 2);
                            ScheduleTrain(nowT + dtBetween + dtWithin, 1);
                        }
                        else
                        {
                            ScheduleTrain(nowT, 2);
                            ScheduleTrain(nowT + dtWithin, 1);

                            ScheduleTrain(nowT + dtBetween, 0);
                            ScheduleTrain(nowT + dtBetween + dtWithin, 1);
                        }

                        trainsScheduled = 4;
                        return dtBetween + dtWithin;
                    }

                case PatternKind.ExpressDouble:
                    {
                        int maxCountByBudget = Mathf.Max(1, tokenBudget / 2);
                        int count = Mathf.Clamp(countRand, 1, maxCountByBudget);

                        if (count <= 1 && tokenBudget < 2) goto case PatternKind.Single;
                        if (tokenBudget < 2) goto case PatternKind.Single;

                        ChooseParallelDouble(out int a, out int b);
                        float t = nowT;

                        int total = 0;
                        for (int i = 0; i < count; i++)
                        {
                            if (total + 2 > tokenBudget) break;
                            ScheduleTrain(t, a);
                            ScheduleTrain(t, b);
                            total += 2;
                            t += dtShort;
                        }

                        if (total <= 0) goto case PatternKind.ParallelDouble;

                        trainsScheduled = total;
                        int pairs = total / 2;
                        return Mathf.Max(0f, (pairs - 1) * dtShort);
                    }

                case PatternKind.MultiSwap:
                    {
                        int maxCountByBudget = Mathf.Max(1, tokenBudget / 4);
                        int count = Mathf.Clamp(countRand, 1, maxCountByBudget);

                        if (tokenBudget < 4)
                        {
                            if (tokenBudget >= 2) goto case PatternKind.StairsExternal;
                            goto case PatternKind.Single;
                        }

                        bool lmToMr = UnityEngine.Random.value < 0.5f;
                        float t = nowT;

                        int total = 0;

                        for (int k = 0; k < count; k++)
                        {
                            if (total + 4 > tokenBudget) break;

                            if (lmToMr)
                            {
                                ScheduleTrain(t, 0); ScheduleTrain(t, 1);
                                ScheduleTrain(t + dtShort, 1); ScheduleTrain(t + dtShort, 2);
                            }
                            else
                            {
                                ScheduleTrain(t, 1); ScheduleTrain(t, 2);
                                ScheduleTrain(t + dtShort, 0); ScheduleTrain(t + dtShort, 1);
                            }

                            total += 4;
                            t += dtShort * 2.0f;
                        }

                        if (total <= 0) goto case PatternKind.SwapSingle;

                        trainsScheduled = total;

                        int blocks = total / 4;
                        return Mathf.Max(0f, (blocks - 1) * dtShort * 2.0f + dtShort);
                    }

                default:
                    throw new InvalidOperationException($"SteamRush: unknown pattern kind {pat.kind}");
            }
        }

        private static void ChooseParallelDouble(out int a, out int b)
        {
            int r = UnityEngine.Random.Range(0, 3);
            if (r == 0) { a = 0; b = 1; return; }
            if (r == 1) { a = 1; b = 2; return; }
            a = 0; b = 2;
        }

        private int RandomLaneAny() => UnityEngine.Random.Range(0, LaneCount);

        private void ScheduleTrain(float startT, int lane)
        {
            float impactT = startT + _approachSeconds;
            float endT = impactT + _collisionWindowSeconds;

            float whistleT = Mathf.Clamp(
                startT + _approachSeconds * _whistleMoment01,
                startT + 0.10f,
                impactT - 0.08f
            );

            var t = new Train
            {
                lane = lane,

                startT = startT,
                whistleT = whistleT,
                impactT = impactT,
                endT = endT,

                introPlayed = false,
                whistlePlayed = false,
                impactResolved = false,
                passStarted = false,

                loopStarted = false,

                crossfadeStartT = -1f,
                crossfadeEndT = -1f,

                introHandle = null,
                loopHandle = null,
                whistleHandle = null,
                passHandle = null
            };

            _trains.Add(t);
            _trainsSpawned++;
        }

        private int ResolveMaxConcurrent(float elapsed)
        {
            if (IsTutorialMode(_modeId))
                return 1;

            if (IsEasyMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.45f) return 1;
                return 2;
            }

            if (IsMediumMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.33f) return 1;
                if (p < 0.66f) return 2;
                return 4;
            }

            if (IsHardMode(_modeId))
            {
                float p = ResolveProgress01(elapsed);
                if (p < 0.25f) return 2;
                if (p < 0.70f) return 3;
                return 4;
            }

            if (IsEndlessMode(_modeId))
            {
                float p = ResolveEndlessProgress01(elapsed);
                if (p < 0.25f) return 2;
                if (p < 0.60f) return 3;
                return 4;
            }

            return 2;
        }

        private float ResolveProgress01(float elapsed)
        {
            if (_hasWinCondition && _durationSeconds > 0.01f)
                return Mathf.Clamp01(elapsed / _durationSeconds);

            float denom = Mathf.Max(1f, _approachSeconds * 20f);
            return Mathf.Clamp01(elapsed / denom);
        }

        private float ResolveEndlessProgress01(float elapsed)
        {
            float denom = Mathf.Max(20f, _endlessVirtualDurationSeconds);
            return Mathf.Clamp01(elapsed / denom);
        }

        private int CountActive(float nowT)
        {
            int c = 0;
            for (int i = 0; i < _trains.Count; i++)
            {
                var t = _trains[i];
                if (nowT >= t.startT && nowT < t.endT)
                    c++;
            }
            return c;
        }

        private void TickTrains(float nowT)
        {
            if (_trains.Count == 0)
                return;

            int concurrent = 0;

            int lead1 = -1, lead2 = -1;
            SelectTop2Leads(nowT, ref lead1, ref lead2);

            for (int i = _trains.Count - 1; i >= 0; i--)
            {
                var t = _trains[i];

                bool active = (nowT >= t.startT && nowT < t.endT);
                if (active) concurrent++;

                if (!t.introPlayed && nowT >= t.startT)
                {
                    t.introPlayed = true;
                    t.introHandle = PlayTrainControlled(t.lane, SfxTrainIntro, volumeMul01: 0.95f, isWhistle: false, nowT: nowT);
                }

                bool shouldLoop = active && (i == lead1 || i == lead2);

                if (shouldLoop)
                {
                    if (!t.loopStarted)
                    {
                        t.loopStarted = true;
                        t.loopHandle = PlayTrainLoopControlled(t.lane, nowT);
                    }
                }
                else
                {
                    if (t.loopStarted)
                    {
                        TryStopHandle(t.loopHandle);
                        t.loopStarted = false;
                        t.loopHandle = null;
                    }
                }

                if (!t.whistlePlayed)
                {
                    bool beforeImpact = nowT < t.impactT;
                    bool canWhistleByTime = nowT >= t.whistleT;
                    bool onSameLane = t.lane == _currentLane;

                    if (beforeImpact && onSameLane && canWhistleByTime)
                    {
                        _duckUntilGameTime = Mathf.Max(_duckUntilGameTime, nowT + DuckSeconds);
                        t.whistleHandle = PlayWhistleNoPanControlled(volumeMul01: WhistleMul);
                        t.whistlePlayed = (t.whistleHandle != null);
                        RefreshAllTrainAudioMix();
                    }
                }

                if (!t.impactResolved && nowT >= t.impactT)
                {
                    t.impactResolved = true;

                    if (!t.passStarted)
                    {
                        t.passStarted = true;
                        t.passHandle = PlayTrainPassWindowControlled(t.lane, nowT);

                        float fade = Mathf.Clamp(_collisionWindowSeconds * CrossfadeFracOfCollision, CrossfadeMinSeconds, CrossfadeMaxSeconds);
                        t.crossfadeStartT = nowT;
                        t.crossfadeEndT = Mathf.Min(t.endT, nowT + fade);
                    }

                    if (t.lane == _currentLane)
                    {
                        _collisions++;
                        BeginCollisionFail();
                        return;
                    }
                }

                if (t.passStarted && t.loopStarted && t.crossfadeEndT > t.crossfadeStartT && nowT >= t.crossfadeEndT)
                {
                    TryStopHandle(t.loopHandle);
                    t.loopStarted = false;
                    t.loopHandle = null;
                }

                if (nowT >= t.endT)
                {
                    if (t.loopStarted)
                    {
                        TryStopHandle(t.loopHandle);
                        t.loopStarted = false;
                        t.loopHandle = null;
                    }

                    TryStopHandle(t.passHandle);
                    t.passHandle = null;

                    _trainsPassed++;
                    _trains.RemoveAt(i);
                    continue;
                }

                _trains[i] = t;
            }

            if (concurrent > _maxConcurrentObserved)
                _maxConcurrentObserved = concurrent;

            RefreshAllTrainAudioMix();
        }

        private void SelectTop2Leads(float nowT, ref int lead1, ref int lead2)
        {
            lead1 = -1;
            lead2 = -1;

            float best1 = float.PositiveInfinity;
            float best2 = float.PositiveInfinity;

            for (int i = 0; i < _trains.Count; i++)
            {
                var t = _trains[i];
                float tti = t.impactT - nowT;
                if (tti < -0.02f) continue;

                if (tti < best1)
                {
                    best2 = best1; lead2 = lead1;
                    best1 = tti; lead1 = i;
                }
                else if (tti < best2)
                {
                    best2 = tti; lead2 = i;
                }
            }
        }

        private void TickWinCondition(float nowT)
        {
            if (!_hasWinCondition)
                return;

            if (_finishQueued)
                return;

            if (_durationSeconds > 0f && nowT >= _durationSeconds)
                BeginWin();
        }

        private void BeginWin()
        {
            if (_finishQueued)
                return;

            _finishQueued = true;

            StopAllHandlesAndClear();
            _trains.Clear();

            TryPlayControlled(SfxWin, AudioFxPlayOptions.Default);

            StopWinRoutineIfAny();
            _winCo = StartCoroutine(WinRoutine());
        }

        private IEnumerator WinRoutine()
        {
            float t = 0f;
            float d = Mathf.Max(0.25f, WinExitDelaySeconds);

            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            FinishCompleted();
            _winCo = null;
        }

        private void StopWinRoutineIfAny()
        {
            if (_winCo != null)
            {
                StopCoroutine(_winCo);
                _winCo = null;
            }
        }

        private void BeginCollisionFail()
        {
            if (_finishQueued)
                return;

            _finishQueued = true;

            StopAllHandlesAndClear();
            PulseHaptic(HapticCollision);

            StopCollisionRoutineIfAny();
            _collisionCo = StartCoroutine(CollisionFailRoutine());
        }

        private IEnumerator CollisionFailRoutine()
        {
            float t = 0f;
            float d = Mathf.Max(0.25f, CollisionExitDelaySeconds);

            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            FinishFailed();
            _collisionCo = null;
        }

        private void StopCollisionRoutineIfAny()
        {
            if (_collisionCo != null)
            {
                StopCoroutine(_collisionCo);
                _collisionCo = null;
            }
        }

        private void RefreshAllTrainAudioMix()
        {
            float nowT = _gameTime;

            _audible.Clear();
            SelectAudibleSetTop2PlusImportant(nowT, _audible);

            _bedE.Clear();
            _bedK.Clear();

            bool hasLeftRel = false;
            bool hasCenterRel = false;
            bool hasRightRel = false;

            for (int idx = 0; idx < _audible.Count; idx++)
            {
                int ti = _audible[idx];
                if (ti < 0 || ti >= _trains.Count) continue;

                var t = _trains[ti];
                if (nowT < t.startT || nowT >= t.endT) continue;

                int delta = t.lane - _currentLane;
                if (delta < 0) hasLeftRel = true;
                else if (delta > 0) hasRightRel = true;
                else hasCenterRel = true;

                BedKind kind = BedKind.None;

                if (t.passHandle != null && t.passStarted && nowT >= t.impactT) kind = BedKind.Pass;
                else if (t.loopHandle != null && t.loopStarted) kind = BedKind.Loop;
                else if (t.introHandle != null && t.introPlayed) kind = BedKind.Intro;

                if (kind == BedKind.None)
                    continue;

                float e = 1.0f;
                if (kind == BedKind.Intro) e = 0.90f;
                else if (kind == BedKind.Loop) e = 0.85f;
                else if (kind == BedKind.Pass) e = 1.00f;

                e *= LaneToRelativeVolume(t.lane, _currentLane);

                if (t.lane == _currentLane)
                    e *= PlayerLaneBedAttenuation;

                if (nowT < _duckUntilGameTime)
                    e *= DuckOtherMul;

                if (t.lane == _currentLane)
                    e *= CenterBoostMul;

                _bedE.Add(e);
                _bedK.Add(kind);
            }

            float L = 0f, R = 0f;

            for (int j = 0; j < _bedE.Count; j++)
            {
                int ti = _audible[j];
                if (ti < 0 || ti >= _trains.Count) continue;

                int trainLane = _trains[ti].lane;
                float e = _bedE[j];

                RelativeGains(trainLane - _currentLane, out float gL, out float gR);

                L += e * gL;
                R += e * gR;
            }

            float maxChan = Mathf.Max(0.0001f, Mathf.Max(L, R));
            float norm = Mathf.Min(1f, BedBudget01 / maxChan);

            bool playerIsMiddle = (_currentLane == 1);
            bool centerPlusOneSide = playerIsMiddle && hasCenterRel && (hasLeftRel ^ hasRightRel);

            for (int i = 0; i < _trains.Count; i++)
            {
                var t = _trains[i];

                if (t.whistleHandle != null)
                {
                    try { t.whistleHandle.SetPan(0f); } catch { }
                    try { t.whistleHandle.SetVolume01(Mathf.Clamp01(WhistleMul)); } catch { }
                }

                int pos = _audible.IndexOf(i);
                if (pos < 0 || pos >= _bedE.Count)
                {
                    ZeroIfAny(t.introHandle);
                    ZeroIfAny(t.loopHandle);
                    ZeroIfAny(t.passHandle);
                    continue;
                }

                float eBase = _bedE[pos] * norm;

                float pan = LaneToPan(t.lane, _currentLane);
                float pitch = ResolveBedPitchMul(t.lane, _currentLane);

                if (centerPlusOneSide && (t.lane == _currentLane))
                {
                    if (hasLeftRel) { pan = Mathf.Clamp(pan + 0.12f, -1f, 1f); pitch *= 0.99f; }
                    else if (hasRightRel) { pan = Mathf.Clamp(pan - 0.12f, -1f, 1f); pitch *= 1.01f; }
                }

                float passAlpha = 0f;
                if (t.passStarted && t.crossfadeEndT > t.crossfadeStartT && nowT >= t.crossfadeStartT && nowT < t.crossfadeEndT)
                    passAlpha = Mathf.Clamp01((nowT - t.crossfadeStartT) / (t.crossfadeEndT - t.crossfadeStartT));
                else if (t.passStarted && nowT >= t.impactT)
                    passAlpha = 1f;

                float preMul = 1f - passAlpha;
                float passMul = passAlpha;

                ApplyBedHandle(t.introHandle, pan, pitch, eBase * preMul);
                ApplyBedHandle(t.loopHandle, pan, pitch, eBase * preMul);
                ApplyBedHandle(t.passHandle, pan, pitch, eBase * passMul);
            }
        }

        private void SelectAudibleSetTop2PlusImportant(float nowT, List<int> outIdx)
        {
            outIdx.Clear();

            int lead1 = -1, lead2 = -1;
            SelectTop2Leads(nowT, ref lead1, ref lead2);

            if (lead1 >= 0) outIdx.Add(lead1);
            if (lead2 >= 0 && lead2 != lead1) outIdx.Add(lead2);

            for (int i = 0; i < _trains.Count; i++)
            {
                if (outIdx.Contains(i)) continue;

                var t = _trains[i];
                if (nowT < t.startT || nowT >= t.endT) continue;

                bool important = (t.passHandle != null && t.passStarted && nowT >= t.impactT);
                if (important)
                    outIdx.Add(i);

                if (outIdx.Count >= 4)
                    break;
            }
        }

        private static void RelativeGains(int delta, out float gL, out float gR)
        {
            if (delta < 0)
            {
                gL = GainEdgeMain;
                gR = GainEdgeBleed;
                return;
            }

            if (delta > 0)
            {
                gL = GainEdgeBleed;
                gR = GainEdgeMain;
                return;
            }

            gL = GainCenter;
            gR = GainCenter;
        }

        private static float ResolveBedPitchMul(int trainLane, int playerLane)
        {
            int dist = Mathf.Abs(trainLane - playerLane);

            float p = 1.0f;

            if (trainLane == playerLane)
                p *= PitchCenterMul;

            int delta = trainLane - playerLane;
            if (delta < 0) p *= (1.0f + PitchSideDelta);
            else if (delta > 0) p *= (1.0f - PitchSideDelta);

            if (dist >= 2)
            {
                if (delta < 0) p *= (1.0f + PitchFarExtra);
                else if (delta > 0) p *= (1.0f - PitchFarExtra);
                else p *= 0.985f;
            }

            return Mathf.Clamp(p, 0.90f, 1.10f);
        }

        private static void ApplyBedHandle(AudioFxHandle h, float pan, float pitchMul, float vol01)
        {
            if (h == null) return;

            try { h.SetPan(pan); } catch { }
            try { h.SetPitch(pitchMul); } catch { }
            try { h.SetVolume01(Mathf.Clamp01(vol01)); } catch { }
        }

        private static void ZeroIfAny(AudioFxHandle h)
        {
            if (h == null) return;
            try { h.SetVolume01(0f); } catch { }
        }

        private void TriggerWhistlesOnLaneChangeIfNeeded()
        {
            float nowT = _gameTime;

            for (int i = 0; i < _trains.Count; i++)
            {
                var t = _trains[i];

                if (nowT < t.startT || nowT >= t.impactT)
                    continue;

                if (t.whistlePlayed)
                    continue;

                if (t.lane != _currentLane)
                    continue;

                if (nowT >= t.whistleT)
                {
                    _duckUntilGameTime = Mathf.Max(_duckUntilGameTime, nowT + DuckSeconds);
                    t.whistleHandle = PlayWhistleNoPanControlled(volumeMul01: WhistleMul);
                    t.whistlePlayed = (t.whistleHandle != null);

                    _trains[i] = t;
                }
            }
        }

        private void PlayMoveSfx(int delta)
        {
            string id = delta > 0 ? SfxSwishRight : SfxSwishLeft;

            var opt = AudioFxPlayOptions.Default;

            opt.PanStereo = delta > 0 ? SwipePanAbs : -SwipePanAbs;
            opt.Volume01 = 0.85f;

            TryPlayControlled(id, opt);
        }

        private AudioFxHandle PlayWhistleNoPanControlled(float volumeMul01)
        {
            if (_audioFx == null) return null;

            var opt = AudioFxPlayOptions.Default;

            opt.PanStereo = 0f;
            opt.Volume01 = Mathf.Clamp01(volumeMul01);

            return TryPlayControlled(SfxWhistle, opt);
        }

        private AudioFxHandle PlayTrainControlled(int trainLane, string soundId, float volumeMul01, bool isWhistle, float nowT)
        {
            if (string.IsNullOrWhiteSpace(soundId) || _audioFx == null)
                return null;

            var opt = AudioFxPlayOptions.Default;

            opt.PanStereo = isWhistle ? 0f : LaneToPan(trainLane, _currentLane);

            float vol = LaneToRelativeVolume(trainLane, _currentLane);
            if (!isWhistle && trainLane == _currentLane) vol *= CenterBoostMul;
            if (!isWhistle && nowT < _duckUntilGameTime) vol *= DuckOtherMul;

            opt.Volume01 = Mathf.Clamp01(vol * Mathf.Clamp01(volumeMul01));

            var h = TryPlayControlled(soundId, opt);
            RefreshAllTrainAudioMix();
            return h;
        }

        private AudioFxHandle PlayTrainLoopControlled(int trainLane, float nowT)
        {
            if (_audioFx == null)
                return null;

            var opt = AudioFxPlayOptions.Default;
            opt.PanStereo = LaneToPan(trainLane, _currentLane);
            opt.Volume01 = 0.60f;
            opt.Loop = true;

            var h = TryPlayControlled(SfxTrainLoop, opt);
            RefreshAllTrainAudioMix();
            return h;
        }

        private AudioFxHandle PlayTrainPassWindowControlled(int trainLane, float nowT)
        {
            if (_audioFx == null)
                return null;

            var opt = AudioFxPlayOptions.Default;
            opt.PanStereo = LaneToPan(trainLane, _currentLane);

            opt.Volume01 = 0.0f;

            opt.StartTimeSeconds = 0f;
            opt.EndTimeSeconds = Mathf.Max(0.05f, _collisionWindowSeconds);

            var h = TryPlayControlled(SfxTrainPass, opt);
            RefreshAllTrainAudioMix();
            return h;
        }

        private AudioFxHandle TryPlayControlled(string soundId, AudioFxPlayOptions opt)
        {
            try { opt.Clamp(); } catch { }

            try
            {
                var h = _audioFx.PlayCurrentGameSoundControlled(soundId, opt);
                if (h != null) _handles.Add(h);
                return h;
            }
            catch { }

            try
            {
                var h = _audioFx.PlayGameSoundControlled(GameId, soundId, opt);
                if (h != null) _handles.Add(h);
                return h;
            }
            catch { }

            return null;
        }

        private void PauseAllHandles()
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                try { _handles[i]?.Pause(); } catch { }
            }
        }

        private void ResumeAllHandles()
        {
            for (int i = 0; i < _handles.Count; i++)
            {
                try { _handles[i]?.Resume(); } catch { }
            }
        }

        private void StopAllHandlesAndClear()
        {
            for (int i = 0; i < _handles.Count; i++)
                TryStopHandle(_handles[i]);

            _handles.Clear();
        }

        private void TryStopHandle(AudioFxHandle h)
        {
            if (h == null) return;
            try { h.Stop(); } catch { }
        }

        private void PruneInvalidHandles()
        {
            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                var h = _handles[i];
                if (h == null)
                {
                    _handles.RemoveAt(i);
                    continue;
                }

                bool valid = false;
                bool playing = false;
                bool paused = false;

                try
                {
                    valid = h.IsValid;
                    playing = h.IsPlaying;
                    paused = h.IsPaused;
                }
                catch
                {
                    valid = false;
                }

                if (!valid)
                {
                    _handles.RemoveAt(i);
                    continue;
                }

                if (!playing && !paused)
                    _handles.RemoveAt(i);
            }
        }

        private static float LaneToPan(int trainLane, int playerLane)
        {
            int delta = trainLane - playerLane;

            float pan = 0f;

            if (delta < 0) pan = -PanAbsNear;
            else if (delta > 0) pan = PanAbsNear;
            else pan = 0f;

            if (Mathf.Abs(delta) >= 2)
                pan = Mathf.Clamp(pan * PanFarBoost, -1f, 1f);

            return pan;
        }

        private static float LaneToRelativeVolume(int trainLane, int playerLane)
        {
            int dist = Mathf.Abs(trainLane - playerLane);
            return dist switch
            {
                0 => VolSameLane,
                1 => VolAdjacentLane,
                _ => VolFarLane
            };
        }

        private void FinishFailed()
        {
            StopAllHandlesAndClear();

            PersistEndlessRunIfNeeded();

            var result = new GameplayGameResult(
                reason: GameplayGameFinishReason.Failed,
                score: 0,
                runtimeStats: GetRuntimeStatsSnapshot()
            );

            try { GameFinished?.Invoke(result); } catch { }
        }

        private void FinishCompleted()
        {
            StopAllHandlesAndClear();

            PersistEndlessRunIfNeeded();

            var result = new GameplayGameResult(
                reason: GameplayGameFinishReason.Completed,
                score: 0,
                runtimeStats: GetRuntimeStatsSnapshot()
            );

            try { GameFinished?.Invoke(result); } catch { }
        }

        public IReadOnlyDictionary<string, string> GetRuntimeStatsSnapshot()
        {
            return new Dictionary<string, string>(capacity: 64)
            {
                ["steamrush.modeId"] = _modeId ?? "",

                ["steamrush.difficultyScale"] = _difficultyScale.ToString("0.00", CultureInfo.InvariantCulture),
                ["steamrush.patternTierMax"] = _patternTierMax.ToString(CultureInfo.InvariantCulture),

                ["steamrush.baseApproachSeconds"] = _baseApproachSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.basePassSeconds"] = _basePassSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.baseSpawnRateScale"] = _baseSpawnRateScale.ToString("0.###", CultureInfo.InvariantCulture),

                ["steamrush.approachSeconds"] = _approachSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.collisionWindowSeconds"] = _collisionWindowSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.spawnRateEffective"] = _spawnRateEffective.ToString("0.###", CultureInfo.InvariantCulture),

                ["steamrush.shortSpacingBaseSeconds"] = _shortSpacingBaseSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.whistleMoment"] = _whistleMoment01.ToString("0.###", CultureInfo.InvariantCulture),

                ["steamrush.prepDelaySeconds"] = _prepDelaySeconds.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.endlessPrepAdaptMul"] = _endlessPrepAdaptMul.ToString("0.###", CultureInfo.InvariantCulture),
                ["steamrush.endlessVirtualDurationSeconds"] = _endlessVirtualDurationSeconds.ToString("0.###", CultureInfo.InvariantCulture),

                ["steamrush.durationSeconds"] = _durationSeconds.ToString("0.0", CultureInfo.InvariantCulture),
                ["steamrush.hasWinCondition"] = _hasWinCondition ? "1" : "0",

                ["steamrush.laneMoves"] = _laneMoves.ToString(CultureInfo.InvariantCulture),
                ["steamrush.outOfBounds"] = _outOfBoundsHits.ToString(CultureInfo.InvariantCulture),

                ["steamrush.trainsSpawned"] = _trainsSpawned.ToString(CultureInfo.InvariantCulture),
                ["steamrush.trainsPassed"] = _trainsPassed.ToString(CultureInfo.InvariantCulture),
                ["steamrush.collisions"] = _collisions.ToString(CultureInfo.InvariantCulture),

                ["steamrush.maxConcurrentObserved"] = _maxConcurrentObserved.ToString(CultureInfo.InvariantCulture),
                ["steamrush.gameTime"] = _gameTime.ToString("0.00", CultureInfo.InvariantCulture),
            };
        }

        private void ResolveServices()
        {
            var services = Core.App.AppContext.Services;

            try { _audioFx = services.Resolve<IAudioFxService>(); } catch { _audioFx = null; }
            try { _haptics = services.Resolve<IHapticsService>(); } catch { _haptics = null; }
            try { _visualMode = services.Resolve<IVisualModeService>(); } catch { _visualMode = null; }
            try { _loc = services.Resolve<ILocalizationService>(); } catch { _loc = null; }
            try { _settings = services.Resolve<ISettingsService>(); } catch { _settings = null; }
        }

        private void ApplyVisualVisibility(bool? forceVisible = null)
        {
            bool wantVisible =
                forceVisible.HasValue ? forceVisible.Value :
                (_running && !_paused && _visualMode != null && _visualMode.Mode == VisualMode.VisualAssist);
        }

        private void PulseHaptic(HapticLevel level)
        {
            if (_haptics == null) return;
            try { _haptics.Pulse(level); } catch { }
        }

        private static bool IsTutorialMode(string modeId)
            => string.Equals(modeId, "tutorial", StringComparison.OrdinalIgnoreCase);

        private static bool IsEasyMode(string modeId)
            => string.Equals(modeId, "easy", StringComparison.OrdinalIgnoreCase);

        private static bool IsMediumMode(string modeId)
            => string.Equals(modeId, "medium", StringComparison.OrdinalIgnoreCase);

        private static bool IsHardMode(string modeId)
            => string.Equals(modeId, "hard", StringComparison.OrdinalIgnoreCase);

        private static bool IsEndlessMode(string modeId)
            => string.Equals(modeId, "endless", StringComparison.OrdinalIgnoreCase);

        private static string NormalizeAndValidateModeId(string modeIdRaw)
        {
            if (string.IsNullOrWhiteSpace(modeIdRaw))
                throw new InvalidOperationException("SteamRushGame: modeId is empty (required).");

            string id = modeIdRaw.Trim();

            if (string.Equals(id, "nieskonczony", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "nieskończony", StringComparison.OrdinalIgnoreCase))
                id = "endless";

            if (string.Equals(id, "łatwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "latwy", StringComparison.OrdinalIgnoreCase))
                id = "easy";

            if (string.Equals(id, "średni", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "sredni", StringComparison.OrdinalIgnoreCase))
                id = "medium";

            if (string.Equals(id, "trudny", StringComparison.OrdinalIgnoreCase))
                id = "hard";

            if (IsTutorialMode(id) || IsEasyMode(id) || IsMediumMode(id) || IsHardMode(id) || IsEndlessMode(id))
                return id.ToLowerInvariant();

            throw new InvalidOperationException($"SteamRushGame: unsupported modeId '{modeIdRaw}'.");
        }

        private static float ResolveTimeFactorFromDifficulty(float difficultyScale)
        {
            float d = Mathf.Max(0.05f, difficultyScale);
            float tf = 1f / d;
            return Mathf.Clamp(tf, 0.60f, 2.20f);
        }

        private static float GetRequiredFloat(IReadOnlyDictionary<string, string> dict, string key, float min, float max)
        {
            if (dict == null)
                throw new InvalidOperationException("SteamRushGame: initialParameters is null.");

            if (!dict.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
                throw new InvalidOperationException($"SteamRushGame: missing required parameter '{key}'.");

            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"SteamRushGame: invalid float for '{key}': '{s}'.");

            if (float.IsNaN(v) || float.IsInfinity(v))
                throw new InvalidOperationException($"SteamRushGame: invalid float (NaN/Inf) for '{key}'.");

            if (v < min || v > max)
                throw new InvalidOperationException($"SteamRushGame: parameter '{key}' out of range [{min}..{max}] value={v.ToString(CultureInfo.InvariantCulture)}.");

            return v;
        }

        private static int GetRequiredInt(IReadOnlyDictionary<string, string> dict, string key, int min, int max)
        {
            if (dict == null)
                throw new InvalidOperationException("SteamRushGame: initialParameters is null.");

            if (!dict.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
                throw new InvalidOperationException($"SteamRushGame: missing required parameter '{key}'.");

            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"SteamRushGame: invalid int for '{key}': '{s}'.");

            if (v < min || v > max)
                throw new InvalidOperationException($"SteamRushGame: parameter '{key}' out of range [{min}..{max}] value={v.ToString(CultureInfo.InvariantCulture)}.");

            return v;
        }

        private float ComputePreparationDelaySeconds()
        {
            if (IsTutorialMode(_modeId))
                return 2.0f;

            float baseSec = 1.0f;
            float d = Mathf.Max(0.10f, _difficultyScale);
            float sec = baseSec / d;

            if (IsEndlessMode(_modeId))
                sec *= _endlessPrepAdaptMul;

            return Mathf.Clamp(sec, 0.45f, 3.00f);
        }

        private void ComputeEndlessAdaptationIfNeeded()
        {
            _endlessPrepAdaptMul = 1.0f;
            _endlessVirtualDurationSeconds = 90f;

            if (!IsEndlessMode(_modeId))
                return;

            float r0 = PlayerPrefs.GetFloat(EndlessRunKey0, 0f);
            float r1 = PlayerPrefs.GetFloat(EndlessRunKey1, 0f);
            float r2 = PlayerPrefs.GetFloat(EndlessRunKey2, 0f);
            float r3 = PlayerPrefs.GetFloat(EndlessRunKey3, 0f);
            float r4 = PlayerPrefs.GetFloat(EndlessRunKey4, 0f);

            float[] a = new float[] { r0, r1, r2, r3, r4 };

            int n = 0;
            float sum = 0f;

            float first = 0f;
            float last = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] <= 0.01f) continue;
                if (n == 0) first = a[i];
                last = a[i];
                sum += a[i];
                n++;
            }

            if (n <= 0)
            {
                _endlessPrepAdaptMul = 1.0f;
                _endlessVirtualDurationSeconds = 90f;
                return;
            }

            float mean = sum / n;
            float trend = 0f;

            if (n >= 2)
                trend = (last - first) / Mathf.Max(1f, mean);

            float prepMul = Mathf.Clamp(1.0f - trend * 0.50f, 0.70f, 1.30f);
            _endlessPrepAdaptMul = prepMul;

            _endlessVirtualDurationSeconds = Mathf.Clamp(mean * 1.20f, 45f, 180f);
        }

        private void PersistEndlessRunIfNeeded()
        {
            if (!IsEndlessMode(_modeId))
                return;

            float v = Mathf.Max(0f, _gameTime);

            float r0 = PlayerPrefs.GetFloat(EndlessRunKey0, 0f);
            float r1 = PlayerPrefs.GetFloat(EndlessRunKey1, 0f);
            float r2 = PlayerPrefs.GetFloat(EndlessRunKey2, 0f);
            float r3 = PlayerPrefs.GetFloat(EndlessRunKey3, 0f);
            float r4 = PlayerPrefs.GetFloat(EndlessRunKey4, 0f);

            PlayerPrefs.SetFloat(EndlessRunKey0, r1);
            PlayerPrefs.SetFloat(EndlessRunKey1, r2);
            PlayerPrefs.SetFloat(EndlessRunKey2, r3);
            PlayerPrefs.SetFloat(EndlessRunKey3, r4);
            PlayerPrefs.SetFloat(EndlessRunKey4, v);

            try { PlayerPrefs.Save(); } catch { }
        }
    }
}