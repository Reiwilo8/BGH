using System;
using System.Collections.Generic;

namespace Project.Core.App
{
    public sealed class GameRunContextService : IGameRunContextService
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyParams =
            new Dictionary<string, string>();

        private GameRunContext _current;

        public bool HasPreparedRun => _current.IsPrepared;
        public bool HasStartedRun => _current.IsStarted;

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

        public bool FinishRun(
            GameRunFinishReason reason,
            bool completed,
            int score = 0,
            DateTime? finishedUtc = null)
        {
            if (!HasPreparedRun || !HasStartedRun)
                return false;

            var fin = finishedUtc ?? DateTime.UtcNow;
            var started = _current.StartedUtc ?? _current.PreparedUtc;

            var dur = fin >= started ? (fin - started) : TimeSpan.Zero;

            var ev = new GameRunFinished(
                context: _current,
                reason: reason,
                completed: completed,
                score: score,
                finishedUtc: fin,
                duration: dur
            );

            RunFinished?.Invoke(ev);

            Clear();
            return true;
        }

        public void Clear()
        {
            _current = default;
        }
    }
}