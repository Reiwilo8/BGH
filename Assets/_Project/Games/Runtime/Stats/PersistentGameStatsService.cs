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
                        bestCompletedTime: m.bestCompletedTimeTicks > 0 ? TimeSpan.FromTicks(m.bestCompletedTimeTicks) : (TimeSpan?)null,
                        bestSurvivalTime: m.bestSurvivalTimeTicks > 0 ? TimeSpan.FromTicks(m.bestSurvivalTimeTicks) : (TimeSpan?)null,
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
            if (!IsValidKey(gameId) || !IsValidKey(modeId))
                return;

            var m = GetOrCreateMode(gameId, modeId);
            m.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            _store.Save();
        }

        public void RecordRunFinished(
            string gameId,
            string modeId,
            TimeSpan duration,
            int score,
            bool completed,
            IReadOnlyDictionary<string, string> runtimeStats = null)
        {
            if (!IsValidKey(gameId) || !IsValidKey(modeId))
                return;

            var m = GetOrCreateMode(gameId, modeId);

            m.runs++;
            if (completed) m.completions++;

            m.lastPlayedUtcTicks = DateTime.UtcNow.Ticks;

            if (duration > TimeSpan.Zero)
            {
                long ticks = duration.Ticks;

                if (m.bestSurvivalTimeTicks <= 0 || ticks > m.bestSurvivalTimeTicks)
                    m.bestSurvivalTimeTicks = ticks;

                if (completed)
                {
                    if (m.bestCompletedTimeTicks <= 0 || ticks < m.bestCompletedTimeTicks)
                        m.bestCompletedTimeTicks = ticks;
                }
            }

            var rr = new RecentRunData
            {
                durationTicks = duration.Ticks,
                score = score,
                completed = completed,
                finishedUtcTicks = DateTime.UtcNow.Ticks,
                runtimeStats = BuildRuntimeStatsList(runtimeStats)
            };

            m.recentRuns.Insert(0, rr);

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

            foreach (var m in g.stats.modes)
                if (m.modeId == modeId)
                    return m;

            var nm = new GameModeStatsData { modeId = modeId };
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
                if (r == null) continue;

                list.Add(new RecentRunSnapshot(
                    duration: r.durationTicks > 0 ? TimeSpan.FromTicks(r.durationTicks) : TimeSpan.Zero,
                    score: r.score,
                    completed: r.completed,
                    finishedUtc: r.finishedUtcTicks > 0
                        ? new DateTime(r.finishedUtcTicks, DateTimeKind.Utc)
                        : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                    runtimeStats: BuildRuntimeStatsDict(r.runtimeStats)
                ));
            }

            return list;
        }

        private static List<GameKeyValueEntry> BuildRuntimeStatsList(IReadOnlyDictionary<string, string> dict)
        {
            if (dict == null || dict.Count == 0)
                return new List<GameKeyValueEntry>();

            var list = new List<GameKeyValueEntry>(dict.Count);

            foreach (var kv in dict)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                list.Add(new GameKeyValueEntry
                {
                    key = kv.Key,
                    value = kv.Value ?? ""
                });
            }

            list.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

            return list;
        }

        private static IReadOnlyDictionary<string, string> BuildRuntimeStatsDict(List<GameKeyValueEntry> list)
        {
            if (list == null || list.Count == 0)
                return null;

            var dict = new Dictionary<string, string>(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || string.IsNullOrWhiteSpace(e.key))
                    continue;

                dict[e.key] = e.value ?? "";
            }

            return dict.Count == 0 ? null : dict;
        }

        private static bool IsValidKey(string s) => !string.IsNullOrWhiteSpace(s);
    }
}