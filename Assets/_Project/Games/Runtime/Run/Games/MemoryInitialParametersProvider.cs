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
            if (_store == null) return;
            if (initialParameters == null) return;
            if (string.IsNullOrWhiteSpace(gameId)) return;

            if (!string.Equals(gameId, MemoryGameId, StringComparison.OrdinalIgnoreCase))
                return;

            AppendMemory(gameId, NormalizeModeId(modeId), initialParameters);
        }

        private void AppendMemory(string gameId, string modeId, IDictionary<string, string> dict)
        {
            var g = SafeGetOrCreateGame(gameId);
            if (g == null) return;

            bool isCustom = string.Equals(modeId, "custom", StringComparison.OrdinalIgnoreCase);

            string wKey = isCustom ? "custom.board.width" : $"mode.{modeId}.board.width";
            string hKey = isCustom ? "custom.board.height" : $"mode.{modeId}.board.height";

            string width = GetCustomString(g, wKey, "4");
            string height = GetCustomString(g, hKey, "4");

            dict["memory.boardWidth"] = width;
            dict["memory.boardHeight"] = height;
        }

        private static string NormalizeModeId(string modeId)
        {
            return string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();
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

                var decoded = PersistenceValueCodec.DecodePossiblyJsonString(raw);
                return string.IsNullOrWhiteSpace(decoded) ? defaultValue : decoded;
            }

            return defaultValue;
        }
    }
}