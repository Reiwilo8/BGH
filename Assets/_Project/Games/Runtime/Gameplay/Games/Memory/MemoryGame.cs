using Project.Core.Audio;
using Project.Core.Audio.Steps;
using Project.Core.AudioFx;
using Project.Core.AudioFx.Games.Memory;
using Project.Core.Haptics;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.Core.Visual.Games.Memory;
using Project.Games.Gameplay.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Games.Memory
{
    public sealed class MemoryGame : MonoBehaviour,
        IGameplayGame,
        IGameplayInputHandler,
        IGameplayDirection4Handler,
        IGameplayRuntimeStatsProvider,
        IGameplayRepeatHandler
    {
        private const string GameId = "memory";
        private const string GameTable = "Game_Memory";

        private const float HorizontalPanAbs = 0.75f;
        private const float VerticalPitchUp = 1.12f;
        private const float VerticalPitchDown = 0.88f;

        private const float MismatchCoverDelaySeconds = 0.55f;

        private const float FlipToResolveDelaySeconds = 0.16f;
        private const float MatchToWinDelaySeconds = 0.20f;
        private const float WinExitDelaySeconds = 0.75f;

        private enum CardState
        {
            Covered = 0,
            Revealed = 1,
            Matched = 2
        }

        private struct Card
        {
            public int Value;
            public CardState State;
        }

        [SerializeField] private MonoBehaviour visualDriver;
        private IMemoryVisualDriver _visual;

        private IAudioFxService _audioFx;
        private IUiAudioOrchestrator _uiAudio;
        private IHapticsService _haptics;
        private IVisualModeService _visualMode;
        private ILocalizationService _loc;
        private ISettingsService _settings;

        private int _w;
        private int _h;

        private Card[] _cards;

        private int _x;
        private int _y;

        private bool _running;
        private bool _paused;

        private int _moves;
        private int _reveals;
        private int _matches;
        private int _mismatches;

        private bool _hasFirstPick;
        private int _firstIndex;

        private bool _initialized;

        private bool _mismatchResolving;
        private int _mismatchA = -1;
        private int _mismatchB = -1;
        private Coroutine _mismatchCo;

        private Coroutine _resolvePickCo;

        private string _modeId;
        private bool _tutorialOnboardingSpoken;

        public event Action<GameplayGameResult> GameFinished;

        public void Initialize(GameplayGameContext ctx)
        {
            ResolveServices();
            ResolveVisual();

            _initialized = true;

            _modeId = ctx.ModeId ?? "";
            _tutorialOnboardingSpoken = false;

            ReadBoardSize(ctx);
            BuildBoard(ctx);

            _x = 0;
            _y = 0;

            _running = false;
            _paused = false;

            _moves = 0;
            _reveals = 0;
            _matches = 0;
            _mismatches = 0;

            _hasFirstPick = false;
            _firstIndex = -1;

            StopMismatchRoutine();
            StopResolvePickRoutine();

            ApplyVisualVisibility();
            ApplyVisualForCurrent();
        }

        public void StartGame()
        {
            _running = true;
            _paused = false;

            StopMismatchRoutine();
            StopResolvePickRoutine();

            ApplyVisualVisibility();
            ApplyVisualForCurrent();

            if (IsTutorialMode(_modeId) && !_tutorialOnboardingSpoken)
            {
                _tutorialOnboardingSpoken = true;
                SpeakRepeatHintsOnly();
            }
        }

        public void StopGame()
        {
            _running = false;
            _paused = false;

            StopMismatchRoutine();
            StopResolvePickRoutine();

            try { _visual?.SetVisible(false); } catch { }
        }

        public void PauseGame()
        {
            _paused = true;
            ApplyVisualVisibility();
        }

        public void ResumeGame()
        {
            _paused = false;

            ApplyVisualVisibility();
            ApplyVisualForCurrent();
        }

        public void Handle(NavAction action)
        {
            if (!_initialized || !_running || _paused) return;
            if (_mismatchResolving) return;
            if (_resolvePickCo != null) return;

            if (action == NavAction.Confirm)
                TryRevealCurrent();
        }

        public void Handle(NavDirection4 dir)
        {
            if (!_initialized || !_running || _paused) return;
            if (_mismatchResolving) return;
            if (_resolvePickCo != null) return;

            int nx = _x;
            int ny = _y;

            switch (dir)
            {
                case NavDirection4.Left: nx -= 1; break;
                case NavDirection4.Right: nx += 1; break;
                case NavDirection4.Up: ny -= 1; break;
                case NavDirection4.Down: ny += 1; break;
                default: return;
            }

            if (nx < 0 || ny < 0 || nx >= _w || ny >= _h)
            {
                PulseHaptic(HapticLevel.Light);
                return;
            }

            _x = nx;
            _y = ny;

            _moves++;

            PlayMoveSfx(dir);
            ApplyVisualSwipe(dir);
            ApplyVisualVisibility();
            ApplyVisualForCurrent();
        }

        public void OnRepeatRequested()
        {
            if (!_initialized || !_running) return;
            if (_paused) return;
            if (_mismatchResolving) return;
            if (_resolvePickCo != null) return;

            int idx = IndexOf(_x, _y);
            if (!IsValidIndex(idx)) return;

            SpeakRepeatFull(_cards[idx]);
        }

        public IReadOnlyDictionary<string, string> GetRuntimeStatsSnapshot()
        {
            return new Dictionary<string, string>(capacity: 12)
            {
                ["memory.boardWidth"] = _w.ToString(),
                ["memory.boardHeight"] = _h.ToString(),

                ["memory.moves"] = _moves.ToString(),
                ["memory.reveals"] = _reveals.ToString(),

                ["memory.matches"] = _matches.ToString(),
                ["memory.mismatches"] = _mismatches.ToString()
            };
        }

        private void ResolveServices()
        {
            var services = Project.Core.App.AppContext.Services;

            try { _audioFx = services.Resolve<IAudioFxService>(); } catch { _audioFx = null; }
            try { _uiAudio = services.Resolve<IUiAudioOrchestrator>(); } catch { _uiAudio = null; }
            try { _haptics = services.Resolve<IHapticsService>(); } catch { _haptics = null; }
            try { _visualMode = services.Resolve<IVisualModeService>(); } catch { _visualMode = null; }
            try { _loc = services.Resolve<ILocalizationService>(); } catch { _loc = null; }
            try { _settings = services.Resolve<ISettingsService>(); } catch { _settings = null; }
        }

        private void ResolveVisual()
        {
            _visual = null;

            if (visualDriver is IMemoryVisualDriver v0)
            {
                _visual = v0;
                return;
            }

#if UNITY_2023_1_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
#endif
            if (all == null) return;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is IMemoryVisualDriver v)
                {
                    _visual = v;
                    return;
                }
            }
        }

        private void ApplyVisualVisibility()
        {
            if (_visual == null) return;

            bool wantVisible =
                _running &&
                !_paused &&
                _visualMode != null &&
                _visualMode.Mode == VisualMode.VisualAssist;

            try { _visual.SetVisible(wantVisible); } catch { }
        }

        private void ReadBoardSize(GameplayGameContext ctx)
        {
            int w = 4;
            int h = 4;

            try
            {
                if (ctx.InitialParameters != null)
                {
                    if (ctx.InitialParameters.TryGetValue("memory.boardWidth", out var sw) && int.TryParse(sw, out var iw))
                        w = iw;

                    if (ctx.InitialParameters.TryGetValue("memory.boardHeight", out var sh) && int.TryParse(sh, out var ih))
                        h = ih;
                }
            }
            catch { }

            w = Mathf.Clamp(w, 2, 8);
            h = Mathf.Clamp(h, 2, 8);

            if (((w * h) & 1) == 1)
            {
                if (h > 2) h -= 1;
                else if (w > 2) w -= 1;
            }

            if (((w * h) & 1) == 1)
            {
                w = 4;
                h = 4;
            }

            _w = w;
            _h = h;
        }

        private void BuildBoard(GameplayGameContext ctx)
        {
            int total = _w * _h;
            int pairs = total / 2;

            var values = new List<int>(total);
            for (int i = 1; i <= pairs; i++)
            {
                values.Add(i);
                values.Add(i);
            }

            int seed = 0;
            bool hasSeed = false;

            try
            {
                if (ctx.Seed.HasValue)
                {
                    seed = ctx.Seed.Value;
                    hasSeed = true;
                }
            }
            catch { }

            var rnd = hasSeed ? new System.Random(seed) : new System.Random();

            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            _cards = new Card[total];
            for (int i = 0; i < total; i++)
            {
                _cards[i] = new Card
                {
                    Value = values[i],
                    State = CardState.Covered
                };
            }
        }

        private void TryRevealCurrent()
        {
            int idx = IndexOf(_x, _y);
            if (!IsValidIndex(idx)) return;

            var c = _cards[idx];

            if (c.State == CardState.Matched)
            {
                SpeakGameKey("card.already_matched");
                return;
            }

            if (c.State == CardState.Revealed)
            {
                SpeakGameKey("card.already_revealed");
                SpeakCardValue(c.Value);
                return;
            }

            c.State = CardState.Revealed;
            _cards[idx] = c;

            _reveals++;

            PlaySfx(MemorySoundIds.CardFlip);
            ApplyVisualVisibility();
            ApplyVisualForCurrent();

            SpeakCardValue(c.Value);

            if (!_hasFirstPick)
            {
                _hasFirstPick = true;
                _firstIndex = idx;
                return;
            }

            if (_firstIndex == idx)
                return;

            StopResolvePickRoutine();
            _resolvePickCo = StartCoroutine(ResolveSecondPickRoutine(secondIdx: idx));
        }

        private IEnumerator ResolveSecondPickRoutine(int secondIdx)
        {
            int firstIdx = _firstIndex;

            _hasFirstPick = false;
            _firstIndex = -1;

            float delay = Mathf.Max(0.02f, FlipToResolveDelaySeconds);

            float t = 0f;
            while (t < delay)
            {
                if (!_running) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_running || !IsValidIndex(firstIdx) || !IsValidIndex(secondIdx))
            {
                _resolvePickCo = null;
                yield break;
            }

            var a = _cards[firstIdx];
            var b = _cards[secondIdx];

            bool match = a.Value == b.Value;

            if (match)
            {
                a.State = CardState.Matched;
                b.State = CardState.Matched;
                _cards[firstIdx] = a;
                _cards[secondIdx] = b;

                _matches++;
                PlaySfx(MemorySoundIds.Match);

                ApplyVisualVisibility();
                ApplyVisualForCurrent();

                if (IsAllMatched())
                {
                    float d2 = Mathf.Max(0f, MatchToWinDelaySeconds);
                    float t2 = 0f;
                    while (t2 < d2)
                    {
                        if (!_running) break;
                        t2 += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    if (_running)
                        PlaySfx(MemorySoundIds.Win);

                    float d3 = Mathf.Max(0f, WinExitDelaySeconds);
                    float t3 = 0f;
                    while (t3 < d3)
                    {
                        if (!_running) break;
                        t3 += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    if (_running)
                        FinishCompleted();
                }

                _resolvePickCo = null;
                yield break;
            }

            _mismatches++;
            PlaySfx(MemorySoundIds.Mismatch);
            PulseHaptic(HapticLevel.Medium);

            BeginMismatchCover(firstIdx, secondIdx);

            _resolvePickCo = null;
        }

        private void BeginMismatchCover(int a, int b)
        {
            StopMismatchRoutine();

            _mismatchResolving = true;
            _mismatchA = a;
            _mismatchB = b;

            _mismatchCo = StartCoroutine(MismatchCoverRoutine());
        }

        private IEnumerator MismatchCoverRoutine()
        {
            float t = 0f;
            float delay = Mathf.Max(0.05f, MismatchCoverDelaySeconds);

            while (t < delay)
            {
                if (!_running) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_running && IsValidIndex(_mismatchA) && IsValidIndex(_mismatchB) && _cards != null)
            {
                var ca = _cards[_mismatchA];
                var cb = _cards[_mismatchB];

                if (ca.State == CardState.Revealed)
                {
                    ca.State = CardState.Covered;
                    _cards[_mismatchA] = ca;
                }

                if (cb.State == CardState.Revealed)
                {
                    cb.State = CardState.Covered;
                    _cards[_mismatchB] = cb;
                }

                ApplyVisualVisibility();
                ApplyVisualForCurrent();
            }

            _mismatchResolving = false;
            _mismatchA = -1;
            _mismatchB = -1;
            _mismatchCo = null;
        }

        private void StopMismatchRoutine()
        {
            if (_mismatchCo != null)
            {
                StopCoroutine(_mismatchCo);
                _mismatchCo = null;
            }

            _mismatchResolving = false;
            _mismatchA = -1;
            _mismatchB = -1;
        }

        private void StopResolvePickRoutine()
        {
            if (_resolvePickCo != null)
            {
                StopCoroutine(_resolvePickCo);
                _resolvePickCo = null;
            }
        }

        private bool IsAllMatched()
        {
            if (_cards == null) return false;

            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].State != CardState.Matched)
                    return false;
            }

            return true;
        }

        private void FinishCompleted()
        {
            var result = new GameplayGameResult(
                reason: GameplayGameFinishReason.Completed,
                score: 0,
                runtimeStats: GetRuntimeStatsSnapshot()
            );

            try { GameFinished?.Invoke(result); } catch { }
        }

        private void ApplyVisualSwipe(NavDirection4 dir)
        {
            if (_visual == null) return;

            try
            {
                switch (dir)
                {
                    case NavDirection4.Left: _visual.Swipe(MemoryVisualSwipe.Right); break;
                    case NavDirection4.Right: _visual.Swipe(MemoryVisualSwipe.Left); break;
                    case NavDirection4.Up: _visual.Swipe(MemoryVisualSwipe.Down); break;
                    case NavDirection4.Down: _visual.Swipe(MemoryVisualSwipe.Up); break;
                }
            }
            catch { }
        }

        private void ApplyVisualForCurrent()
        {
            if (_visual == null) return;
            if (_cards == null) return;

            int idx = IndexOf(_x, _y);
            if (!IsValidIndex(idx)) return;

            var c = _cards[idx];

            try
            {
                switch (c.State)
                {
                    case CardState.Covered:
                        _visual.SetCovered();
                        break;

                    case CardState.Revealed:
                        _visual.SetRevealed(c.Value.ToString());
                        break;

                    case CardState.Matched:
                        _visual.SetMatched(c.Value.ToString());
                        break;
                }
            }
            catch { }
        }

        private void PlayMoveSfx(NavDirection4 dir)
        {
            switch (dir)
            {
                case NavDirection4.Left:
                    {
                        var opt = AudioFxPlayOptions.Default;
                        opt.PanStereo = -HorizontalPanAbs;
                        PlaySfx(MemorySoundIds.HorizontalSwishLeft, opt);
                        break;
                    }
                case NavDirection4.Right:
                    {
                        var opt = AudioFxPlayOptions.Default;
                        opt.PanStereo = HorizontalPanAbs;
                        PlaySfx(MemorySoundIds.HorizontalSwishRight, opt);
                        break;
                    }
                case NavDirection4.Up:
                    {
                        var opt = AudioFxPlayOptions.Default;
                        opt.Pitch = VerticalPitchUp;
                        PlaySfx(MemorySoundIds.VerticalSwishUp, opt);
                        break;
                    }
                case NavDirection4.Down:
                    {
                        var opt = AudioFxPlayOptions.Default;
                        opt.Pitch = VerticalPitchDown;
                        PlaySfx(MemorySoundIds.VerticalSwishDown, opt);
                        break;
                    }
            }
        }

        private void PlaySfx(string soundId)
        {
            PlaySfx(soundId, null);
        }

        private void PlaySfx(string soundId, AudioFxPlayOptions? options)
        {
            if (string.IsNullOrWhiteSpace(soundId)) return;

            try
            {
                if (options.HasValue)
                {
                    var opt = options.Value;
                    try { opt.Clamp(); } catch { }
                    _audioFx?.PlayGameSound(GameId, soundId, opt);
                }
                else
                {
                    _audioFx?.PlayGameSound(GameId, soundId);
                }
            }
            catch
            {
                try
                {
                    if (options.HasValue)
                    {
                        var opt = options.Value;
                        try { opt.Clamp(); } catch { }
                        _audioFx?.PlayCurrentGameSound(soundId, opt);
                    }
                    else
                    {
                        _audioFx?.PlayCurrentGameSound(soundId);
                    }
                }
                catch { }
            }
        }

        private void PulseHaptic(HapticLevel level)
        {
            if (_haptics == null) return;
            try { _haptics.Pulse(level); } catch { }
        }

        private void SpeakCardValue(int value)
        {
            SpeakCoreKey("common.text", value.ToString());
        }

        private void SpeakRepeatFull(Card c)
        {
            if (_uiAudio == null) return;

            string stateKey = c.State switch
            {
                CardState.Covered => "card.covered",
                CardState.Revealed => "card.already_revealed",
                CardState.Matched => "card.already_matched",
                _ => "card.covered"
            };

            bool speakValue = c.State == CardState.Revealed;
            string hintModeKey = ResolveRepeatHintKey();

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => RepeatSequence(ctx, stateKey, speakValue ? c.Value.ToString() : null, hintModeKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private void SpeakRepeatHintsOnly()
        {
            if (_uiAudio == null) return;

            string hintModeKey = ResolveRepeatHintKey();

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => RepeatSequence(ctx, stateKey: null, value: null, hintModeKey: hintModeKey),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private IEnumerator RepeatSequence(UiAudioContext ctx, string stateKey, string value, string hintModeKey)
        {
            if (ctx?.Handle == null || ctx.Handle.IsCancelled)
                yield break;

            if (!string.IsNullOrWhiteSpace(stateKey))
            {
                yield return SpeakGameKeyAndWait(ctx, stateKey);
                if (ctx.Handle.IsCancelled) yield break;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return UiAudioSteps.SpeakKeyAndWait(ctx, "common.text", value);
                    if (ctx.Handle.IsCancelled) yield break;
                }
            }

            yield return SpeakGameKeyAndWait(ctx, "hint");
            if (ctx.Handle.IsCancelled) yield break;

            if (!string.IsNullOrWhiteSpace(hintModeKey))
                yield return SpeakGameKeyAndWait(ctx, hintModeKey);
        }

        private IEnumerator SpeakGameKeyAndWait(UiAudioContext ctx, string key, params object[] args)
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

        private void SpeakCoreKey(string key, params object[] args)
        {
            if (_uiAudio == null || string.IsNullOrWhiteSpace(key))
                return;

            _uiAudio.Play(
                UiAudioScope.Gameplay,
                ctx => UiAudioSteps.SpeakKeyAndWait(ctx, key, args),
                SpeechPriority.Normal,
                interruptible: true
            );
        }

        private string ResolveRepeatHintKey()
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

            return mode == ControlHintMode.Touch
                ? "hint.touch"
                : "hint.keyboard";
        }

        private static bool IsTutorialMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId)) return false;
            return string.Equals(modeId, "tutorial", StringComparison.OrdinalIgnoreCase);
        }

        private int IndexOf(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _w || y >= _h) return -1;
            return (y * _w) + x;
        }

        private bool IsValidIndex(int idx)
        {
            return _cards != null && idx >= 0 && idx < _cards.Length;
        }
    }
}