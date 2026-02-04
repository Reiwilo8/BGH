using System;
using System.Collections.Generic;

namespace Project.Games.Stats
{
    public interface IGameStatsService
    {
        GameStatsSnapshot GetSnapshot(string gameId);

        void RecordRunStarted(string gameId, string modeId);

        void RecordRunFinished(
            string gameId,
            string modeId,
            TimeSpan duration,
            int score,
            bool completed,
            IReadOnlyDictionary<string, string> runtimeStats = null);

        void Reset(string gameId);
    }
}