using System;
using System.Collections.Generic;

namespace Project.Core.App
{
    public enum GameRunFinishReason
    {
        Unknown = 0,
        Completed = 1,
        AbortedByUser = 2,
        Failed = 3,
        Quit = 4
    }

    public readonly struct GameRunContext
    {
        public readonly string GameId;
        public readonly string ModeId;

        public readonly string RunId;

        public readonly int? Seed;

        public readonly DateTime PreparedUtc;

        public readonly DateTime? StartedUtc;

        public readonly IReadOnlyDictionary<string, string> InitialParameters;

        public readonly bool WereRunSettingsCustomized;

        internal GameRunContext(
            string gameId,
            string modeId,
            string runId,
            int? seed,
            DateTime preparedUtc,
            DateTime? startedUtc,
            IReadOnlyDictionary<string, string> initialParameters,
            bool wereRunSettingsCustomized)
        {
            GameId = gameId;
            ModeId = modeId;
            RunId = runId;
            Seed = seed;
            PreparedUtc = preparedUtc;
            StartedUtc = startedUtc;
            InitialParameters = initialParameters;
            WereRunSettingsCustomized = wereRunSettingsCustomized;
        }

        public bool IsPrepared => !string.IsNullOrWhiteSpace(RunId);
        public bool IsStarted => StartedUtc.HasValue;
    }

    public readonly struct GameRunFinished
    {
        public readonly GameRunContext Context;
        public readonly GameRunFinishReason Reason;

        public readonly bool Completed;
        public readonly int Score;

        public readonly DateTime FinishedUtc;
        public readonly TimeSpan Duration;

        public GameRunFinished(
            GameRunContext context,
            GameRunFinishReason reason,
            bool completed,
            int score,
            DateTime finishedUtc,
            TimeSpan duration)
        {
            Context = context;
            Reason = reason;
            Completed = completed;
            Score = score;
            FinishedUtc = finishedUtc;
            Duration = duration;
        }
    }

    public interface IGameRunContextService
    {
        bool HasPreparedRun { get; }
        bool HasStartedRun { get; }

        GameRunContext Current { get; }

        void PrepareRun(
            string gameId,
            string modeId,
            int? seed = null,
            IReadOnlyDictionary<string, string> initialParameters = null,
            bool wereRunSettingsCustomized = false);

        bool StartRun(DateTime? startedUtc = null);

        bool FinishRun(
            GameRunFinishReason reason,
            bool completed,
            int score = 0,
            DateTime? finishedUtc = null);

        void Clear();

        event Action<GameRunContext> RunPrepared;
        event Action<GameRunContext> RunStarted;
        event Action<GameRunFinished> RunFinished;
    }
}