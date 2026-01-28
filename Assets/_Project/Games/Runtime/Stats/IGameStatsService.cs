using System;

namespace Project.Games.Stats
{
    public interface IGameStatsService
    {
        GameStatsSnapshot GetSnapshot(string gameId);

        void RecordRunStarted(string gameId, string modeId);
        void RecordRunFinished(string gameId, string modeId, TimeSpan duration, int score, bool completed);

        void Reset(string gameId);
    }
}