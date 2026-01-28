using System;
using System.Collections.Generic;

namespace Project.Games.Stats
{
    public enum BestTimeDirection
    {
        LowerIsBetter,
        HigherIsBetter
    }

    public readonly struct RecentRunSnapshot
    {
        public readonly TimeSpan Duration;
        public readonly int Score;
        public readonly bool Completed;
        public readonly DateTime FinishedUtc;

        public RecentRunSnapshot(TimeSpan duration, int score, bool completed, DateTime finishedUtc)
        {
            Duration = duration;
            Score = score;
            Completed = completed;
            FinishedUtc = finishedUtc;
        }
    }

    public readonly struct ModeOverallSnapshot
    {
        public readonly int Runs;
        public readonly int Completions;

        public readonly TimeSpan? BestTime;
        public readonly BestTimeDirection BestTimeDir;

        public readonly DateTime? LastPlayedUtc;

        public ModeOverallSnapshot(
            int runs,
            int completions,
            TimeSpan? bestTime,
            BestTimeDirection bestTimeDir,
            DateTime? lastPlayedUtc)
        {
            Runs = runs;
            Completions = completions;
            BestTime = bestTime;
            BestTimeDir = bestTimeDir;
            LastPlayedUtc = lastPlayedUtc;
        }
    }

    public readonly struct ModeStatsSnapshot
    {
        public readonly string ModeId;
        public readonly ModeOverallSnapshot Overall;
        public readonly IReadOnlyList<RecentRunSnapshot> RecentRuns;

        public ModeStatsSnapshot(string modeId, ModeOverallSnapshot overall, IReadOnlyList<RecentRunSnapshot> recentRuns)
        {
            ModeId = modeId;
            Overall = overall;
            RecentRuns = recentRuns;
        }
    }

    public readonly struct GameStatsSnapshot
    {
        public readonly string GameId;
        public readonly IReadOnlyList<ModeStatsSnapshot> PerMode;

        public GameStatsSnapshot(string gameId, IReadOnlyList<ModeStatsSnapshot> perMode)
        {
            GameId = gameId;
            PerMode = perMode;
        }
    }
}