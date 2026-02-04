using System;
using System.Collections.Generic;
using Project.Games.Persistence;

namespace Project.Games.Run
{
    public sealed class MemoryInitialParametersProvider : IGameInitialParametersProvider
    {
        private const string MemoryGameId = "memory";

        private readonly IGameDataStore _store;

        public MemoryInitialParametersProvider(IGameDataStore store)
        {
            _store = store;
        }

        public void AppendParameters(string gameId, string modeId, IDictionary<string, string> initialParameters)
        {
            if (initialParameters == null) return;
            if (string.IsNullOrWhiteSpace(gameId)) return;

            if (!string.Equals(gameId, MemoryGameId, StringComparison.OrdinalIgnoreCase))
                return;

            if (_store == null)
            {
                AppendDefaults(NormalizeModeId(modeId), initialParameters);
                return;
            }

            AppendFromStoreOrDefaults(gameId, NormalizeModeId(modeId), initialParameters);
        }

        private void AppendFromStoreOrDefaults(string gameId, string modeId, IDictionary<string, string> dict)
        {
            var g = SafeGetOrCreateGame(gameId);
            if (g == null)
            {
                AppendDefaults(modeId, dict);
                return;
            }

            bool isCustom = string.Equals(modeId, "custom", StringComparison.OrdinalIgnoreCase);

            string wKey = isCustom ? "custom.board.width" : $"mode.{modeId}.board.width";
            string hKey = isCustom ? "custom.board.height" : $"mode.{modeId}.board.height";

            ResolveDefaultBoardSize(modeId, out string defW, out string defH);

            string width = GetCustomString(g, wKey, defW);
            string height = GetCustomString(g, hKey, defH);

            dict["memory.boardWidth"] = width;
            dict["memory.boardHeight"] = height;
        }

        private static void AppendDefaults(string modeId, IDictionary<string, string> dict)
        {
            ResolveDefaultBoardSize(modeId, out string w, out string h);
            dict["memory.boardWidth"] = w;
            dict["memory.boardHeight"] = h;
        }

        private static string NormalizeModeId(string modeId)
        {
            return string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();
        }

        private static void ResolveDefaultBoardSize(string modeId, out string width, out string height)
        {
            width = "4";
            height = "4";

            if (string.IsNullOrWhiteSpace(modeId))
                return;

            if (string.Equals(modeId, "tutorial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "samouczek", StringComparison.OrdinalIgnoreCase))
            {
                width = "2";
                height = "2";
                return;
            }

            if (string.Equals(modeId, "easy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "latwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "³atwy", StringComparison.OrdinalIgnoreCase))
            {
                width = "4";
                height = "2";
                return;
            }

            if (string.Equals(modeId, "medium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "sredni", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "œredni", StringComparison.OrdinalIgnoreCase))
            {
                width = "4";
                height = "3";
                return;
            }

            if (string.Equals(modeId, "hard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "trudny", StringComparison.OrdinalIgnoreCase))
            {
                width = "4";
                height = "4";
                return;
            }

            if (string.Equals(modeId, "custom", StringComparison.OrdinalIgnoreCase))
            {
                width = "6";
                height = "6";
                return;
            }
        }

        private GameUserEntry SafeGetOrCreateGame(string gameId)
        {
            try { return _store.GetOrCreateGame(gameId); }
            catch { return null; }
        }

        private static string GetCustomString(GameUserEntry g, string key, string defaultValue)
        {
            if (g == null || g.custom == null || string.IsNullOrWhiteSpace(key))
                return defaultValue;

            for (int i = 0; i < g.custom.Count; i++)
            {
                var e = g.custom[i];
                if (e == null || e.key != key)
                    continue;

                var raw = e.jsonValue;
                if (string.IsNullOrWhiteSpace(raw))
                    return defaultValue;

                string decoded;
                try { decoded = PersistenceValueCodec.DecodePossiblyJsonString(raw); }
                catch { decoded = null; }

                return string.IsNullOrWhiteSpace(decoded) ? defaultValue : decoded;
            }

            return defaultValue;
        }
    }
}