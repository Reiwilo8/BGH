using System;
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

            Normalize();
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public GameUserEntry GetOrCreateGame(string gameId)
        {
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
            string id = NormalizeGameId(gameId);

            for (int i = Data.games.Count - 1; i >= 0; i--)
            {
                if (Data.games[i] != null && Data.games[i].gameId == id)
                    Data.games.RemoveAt(i);
            }
        }

        private void Normalize()
        {
            if (Data.games == null)
                Data.games = new System.Collections.Generic.List<GameUserEntry>();

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
            }
        }

        private static string NormalizeGameId(string gameId)
        {
            return string.IsNullOrWhiteSpace(gameId) ? "unknown" : gameId;
        }
    }
}