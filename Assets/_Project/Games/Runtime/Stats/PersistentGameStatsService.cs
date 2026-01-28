using System;
using System.Collections.Generic;
using Project.Games.Persistence;

namespace Project.Games.Stats
{
    public sealed class PersistentGameStatsService : IGameStatsService
    {
        private const int MaxRecentHistory = 50;

        private readonly IGameDataStore _store;

        public PersistentGameStatsService(IGameDataStore store)
        {
            _store = store;
        }

        public GameStatsSnapshot GetSnapshot(string gameId)
        {
            var g = _store.GetOrCreateGame(gameId);
            var perMode = new List<ModeStatsSnapshot>();

            foreach (var m in g.stats.modes)
            {
                perMode.Add(new ModeStatsSnapshot(
                    modeId: m.modeId,
                    overall: new ModeOverallSnapshot(
                        runs: m.runs,
                        completions: m.completions,
                        bestTime: m.bestTimeTicks > 0 ? TimeSpan.FromTicks(m.bestTimeTicks) : (TimeSpan?)null,
                        bestTimeDir: m.bestTimeDir,
                        lastPlayedUtc: m.lastPlayedUtcTicks > 0 ? new DateTime(m.lastPlayedUtcTicks, DateTimeKind.Utc) : (DateTime?)null
                    ),
                    recentRuns: BuildRecent(m)
                ));
            }

            perMode.Sort((a, b) => string.CompareOrdinal(a.ModeId, b.ModeId));
            return new GameStatsSnapshot(gameId, perMode);
        }

        public void RecordRunStarted(string gameId, string modeId)
        {
            var m = GetOrCreateMode(gameId, modeId);
            m.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            _store.Save();
        }

        public void RecordRunFinished(string gameId, string modeId, TimeSpan duration, int score, bool completed)
        {
            var m = GetOrCreateMode(gameId, modeId);

            m.runs++;
            if (completed) m.completions++;

            m.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;

            if (duration > TimeSpan.Zero)
            {
                if (m.bestTimeTicks <= 0)
                    m.bestTimeTicks = duration.Ticks;
                else
                {
                    bool better = m.bestTimeDir == BestTimeDirection.LowerIsBetter
                        ? duration.Ticks < m.bestTimeTicks
                        : duration.Ticks > m.bestTimeTicks;

                    if (better)
                        m.bestTimeTicks = duration.Ticks;
                }
            }

            m.recentRuns.Insert(0, new RecentRunData
            {
                durationTicks = duration.Ticks,
                score = score,
                completed = completed,
                finishedUtcTicks = DateTime.UtcNow.Ticks
            });

            while (m.recentRuns.Count > MaxRecentHistory)
                m.recentRuns.RemoveAt(m.recentRuns.Count - 1);

            _store.Save();
        }

        public void Reset(string gameId)
        {
            _store.RemoveGame(gameId);
            _store.Save();
        }

        private GameModeStatsData GetOrCreateMode(string gameId, string modeId)
        {
            var g = _store.GetOrCreateGame(gameId);
            string id = string.IsNullOrWhiteSpace(modeId) ? "default" : modeId;

            foreach (var m in g.stats.modes)
                if (m.modeId == id)
                    return m;

            var nm = new GameModeStatsData { modeId = id };
            g.stats.modes.Add(nm);
            return nm;
        }

        private static IReadOnlyList<RecentRunSnapshot> BuildRecent(GameModeStatsData m)
        {
            if (m.recentRuns == null || m.recentRuns.Count == 0)
                return Array.Empty<RecentRunSnapshot>();

            var list = new List<RecentRunSnapshot>(m.recentRuns.Count);
            foreach (var r in m.recentRuns)
            {
                list.Add(new RecentRunSnapshot(
                    duration: r.durationTicks > 0 ? TimeSpan.FromTicks(r.durationTicks) : TimeSpan.Zero,
                    score: r.score,
                    completed: r.completed,
                    finishedUtc: new DateTime(r.finishedUtcTicks, DateTimeKind.Utc)
                ));
            }
            return list;
        }
    }
}