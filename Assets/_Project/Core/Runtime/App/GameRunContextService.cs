using System;
using System.Collections.Generic;

namespace Project.Core.App
{
    public sealed class GameRunContextService : IGameRunContextService
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyParams =
            new Dictionary<string, string>();

        private GameRunContext _current;

        private DateTime? _pausedUtc;
        private TimeSpan _pausedAccumulated;

        public bool HasPreparedRun => _current.IsPrepared;
        public bool HasStartedRun => _current.IsStarted;

        public bool IsPaused => _pausedUtc.HasValue;

        public GameRunContext Current => _current;

        public event Action<GameRunContext> RunPrepared;
        public event Action<GameRunContext> RunStarted;
        public event Action<GameRunFinished> RunFinished;

        public void PrepareRun(
            string gameId,
            string modeId,
            int? seed = null,
            IReadOnlyDictionary<string, string> initialParameters = null,
            bool wereRunSettingsCustomized = false)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(modeId))
            {
                Clear();
                return;
            }

            var now = DateTime.UtcNow;

            _pausedUtc = null;
            _pausedAccumulated = TimeSpan.Zero;

            _current = new GameRunContext(
                gameId: gameId,
                modeId: modeId,
                runId: Guid.NewGuid().ToString("N"),
                seed: seed,
                preparedUtc: now,
                startedUtc: null,
                initialParameters: initialParameters ?? EmptyParams,
                wereRunSettingsCustomized: wereRunSettingsCustomized
            );

            RunPrepared?.Invoke(_current);
        }

        public bool StartRun(DateTime? startedUtc = null)
        {
            if (!HasPreparedRun)
                return false;

            if (_current.StartedUtc.HasValue)
                return false;

            var start = startedUtc ?? DateTime.UtcNow;

            _pausedUtc = null;
            _pausedAccumulated = TimeSpan.Zero;

            _current = new GameRunContext(
                gameId: _current.GameId,
                modeId: _current.ModeId,
                runId: _current.RunId,
                seed: _current.Seed,
                preparedUtc: _current.PreparedUtc,
                startedUtc: start,
                initialParameters: _current.InitialParameters ?? EmptyParams,
                wereRunSettingsCustomized: _current.WereRunSettingsCustomized
            );

            RunStarted?.Invoke(_current);
            return true;
        }

        public bool PauseRun(DateTime? pausedUtc = null)
        {
            if (!HasPreparedRun || !HasStartedRun)
                return false;

            if (_pausedUtc.HasValue)
                return false;

            _pausedUtc = pausedUtc ?? DateTime.UtcNow;
            return true;
        }

        public bool ResumeRun(DateTime? resumedUtc = null)
        {
            if (!HasPreparedRun || !HasStartedRun)
                return false;

            if (!_pausedUtc.HasValue)
                return false;

            var resume = resumedUtc ?? DateTime.UtcNow;
            var pausedAt = _pausedUtc.Value;

            if (resume > pausedAt)
                _pausedAccumulated += (resume - pausedAt);

            _pausedUtc = null;
            return true;
        }

        public bool FinishRun(
            GameRunFinishReason reason,
            bool completed,
            int score = 0,
            DateTime? finishedUtc = null,
            IReadOnlyDictionary<string, string> runtimeStats = null)
        {
            if (!HasPreparedRun || !HasStartedRun)
                return false;

            var fin = finishedUtc ?? DateTime.UtcNow;
            var started = _current.StartedUtc ?? _current.PreparedUtc;

            TimeSpan pausedTotal = _pausedAccumulated;

            if (_pausedUtc.HasValue)
            {
                var pausedAt = _pausedUtc.Value;
                if (fin > pausedAt)
                    pausedTotal += (fin - pausedAt);
            }

            TimeSpan raw = fin >= started ? (fin - started) : TimeSpan.Zero;
            TimeSpan effective = raw - pausedTotal;

            if (effective < TimeSpan.Zero)
                effective = TimeSpan.Zero;

            var ev = new GameRunFinished(
                context: _current,
                reason: reason,
                completed: completed,
                score: score,
                finishedUtc: fin,
                duration: effective,
                runtimeStats: runtimeStats
            );

            RunFinished?.Invoke(ev);

            Clear();
            return true;
        }

        public void Clear()
        {
            _current = default;
            _pausedUtc = null;
            _pausedAccumulated = TimeSpan.Zero;
        }
    }
}