using UnityEngine;

namespace Project.Games.Persistence
{
    public sealed class PlayerPrefsGameDataStore : IGameDataStore
    {
        private const string Key = "games_user_data_v1";
        private const int MaxGames = 100;

        public GamesUserData Data { get; private set; }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                Data = new GamesUserData();
                return;
            }

            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                Data = new GamesUserData();
                return;
            }

            try
            {
                Data = JsonUtility.FromJson<GamesUserData>(json) ?? new GamesUserData();
            }
            catch
            {
                Data = new GamesUserData();
            }

            NormalizeAndMigrateIfNeeded();
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public GameUserEntry GetOrCreateGame(string gameId)
        {
            EnsureData();

            string id = NormalizeGameId(gameId);

            foreach (var g in Data.games)
                if (g != null && g.gameId == id)
                    return g;

            var entry = new GameUserEntry { gameId = id };
            Data.games.Add(entry);

            if (Data.games.Count > MaxGames)
                Data.games.RemoveAt(0);

            return entry;
        }

        public void RemoveGame(string gameId)
        {
            EnsureData();

            string id = NormalizeGameId(gameId);

            for (int i = Data.games.Count - 1; i >= 0; i--)
            {
                if (Data.games[i] != null && Data.games[i].gameId == id)
                    Data.games.RemoveAt(i);
            }
        }

        private void EnsureData()
        {
            if (Data == null)
                Data = new GamesUserData();
            if (Data.games == null)
                Data.games = new System.Collections.Generic.List<GameUserEntry>();
        }

        private void NormalizeAndMigrateIfNeeded()
        {
            EnsureData();

            for (int i = Data.games.Count - 1; i >= 0; i--)
            {
                var g = Data.games[i];
                if (g == null || string.IsNullOrWhiteSpace(g.gameId))
                {
                    Data.games.RemoveAt(i);
                    continue;
                }

                g.gameId = NormalizeGameId(g.gameId);

                if (g.prefs == null)
                    g.prefs = new GamePreferencesData();

                if (g.stats == null)
                    g.stats = new GameStatsData();

                if (g.stats.modes == null)
                    g.stats.modes = new System.Collections.Generic.List<GameModeStatsData>();

                if (g.custom == null)
                    g.custom = new System.Collections.Generic.List<GameCustomEntry>();

                if (g.prefs.knownSeeds == null)
                    g.prefs.knownSeeds = new System.Collections.Generic.List<int>();

                for (int mi = g.stats.modes.Count - 1; mi >= 0; mi--)
                {
                    var m = g.stats.modes[mi];
                    if (m == null || string.IsNullOrWhiteSpace(m.modeId))
                    {
                        g.stats.modes.RemoveAt(mi);
                        continue;
                    }

                    if (m.recentRuns == null)
                        m.recentRuns = new System.Collections.Generic.List<RecentRunData>();

                    for (int ri = 0; ri < m.recentRuns.Count; ri++)
                    {
                        var r = m.recentRuns[ri];
                        if (r == null) continue;

                        if (r.runtimeStats == null)
                            r.runtimeStats = new System.Collections.Generic.List<GameKeyValueEntry>();
                    }
                }
            }

            if (Data.schemaVersion < 2)
            {
                RecomputeBestTimesFromRecentRuns();
                Data.schemaVersion = 2;
                Save();
            }

            if (Data.schemaVersion < 3)
            {
                foreach (var g in Data.games)
                {
                    if (g == null || g.prefs == null) continue;
                    if (g.prefs.knownSeeds == null)
                        g.prefs.knownSeeds = new System.Collections.Generic.List<int>();
                }

                Data.schemaVersion = 3;
                Save();
            }

            if (Data.schemaVersion < 4)
            {
                foreach (var g in Data.games)
                {
                    if (g?.stats?.modes == null) continue;

                    foreach (var m in g.stats.modes)
                    {
                        if (m?.recentRuns == null) continue;

                        foreach (var r in m.recentRuns)
                        {
                            if (r == null) continue;
                            if (r.runtimeStats == null)
                                r.runtimeStats = new System.Collections.Generic.List<GameKeyValueEntry>();
                        }
                    }
                }

                Data.schemaVersion = 4;
                Save();
            }

            if (Data.schemaVersion < 4)
                Data.schemaVersion = 4;
        }

        private void RecomputeBestTimesFromRecentRuns()
        {
            if (Data.games == null) return;

            foreach (var g in Data.games)
            {
                if (g == null || g.stats == null || g.stats.modes == null)
                    continue;

                foreach (var m in g.stats.modes)
                {
                    if (m == null) continue;

                    long bestSurvival = 0;
                    long bestCompleted = 0;

                    var rr = m.recentRuns;
                    if (rr != null)
                    {
                        foreach (var r in rr)
                        {
                            if (r == null) continue;

                            long dt = r.durationTicks;
                            if (dt <= 0) continue;

                            if (dt > bestSurvival)
                                bestSurvival = dt;

                            if (r.completed)
                            {
                                if (bestCompleted == 0 || dt < bestCompleted)
                                    bestCompleted = dt;
                            }
                        }
                    }

                    if (m.bestSurvivalTimeTicks <= 0 && bestSurvival > 0)
                        m.bestSurvivalTimeTicks = bestSurvival;

                    if (m.bestCompletedTimeTicks <= 0 && bestCompleted > 0)
                        m.bestCompletedTimeTicks = bestCompleted;
                }
            }
        }

        private static string NormalizeGameId(string gameId)
        {
            return string.IsNullOrWhiteSpace(gameId) ? "unknown" : gameId;
        }
    }
}