using System;
using Project.Core.Localization;
using Project.Games.Definitions;

namespace Project.Games.Localization
{
    public static class GameLocalization
    {
        private const string DefaultGameNameKey = "name";
        private const string DefaultGameDescriptionKey = "description";

        public static string GetGameName(ILocalizationService loc, GameDefinition game)
        {
            if (game == null) return "Unknown";

            string table = game.localizationTable;
            string key = string.IsNullOrWhiteSpace(game.nameKey) ? DefaultGameNameKey : game.nameKey;

            var fromTable = GetFromGameTable(loc, table, key);
            if (!string.IsNullOrWhiteSpace(fromTable))
                return fromTable;

            if (!string.IsNullOrWhiteSpace(game.displayName))
                return game.displayName;

            return !string.IsNullOrWhiteSpace(game.gameId) ? game.gameId : "Unknown";
        }

        public static string GetGameDescription(ILocalizationService loc, GameDefinition game)
        {
            if (game == null) return "";

            string table = game.localizationTable;
            string key = string.IsNullOrWhiteSpace(game.descriptionKey) ? DefaultGameDescriptionKey : game.descriptionKey;

            var fromTable = GetFromGameTable(loc, table, key);
            if (!string.IsNullOrWhiteSpace(fromTable))
                return fromTable;

            if (!string.IsNullOrWhiteSpace(game.description))
                return game.description;

            return "";
        }

        public static string GetModeName(ILocalizationService loc, GameModeDefinition mode)
        {
            if (mode == null) return "—";

            var id = mode.modeId;
            if (string.IsNullOrWhiteSpace(id))
                return "—";

            if (loc != null)
            {
                var key = $"mode.{id}";
                var localized = SafeGet(loc, key);

                if (!string.IsNullOrWhiteSpace(localized) && localized != key)
                    return localized;
            }

            return id;
        }

        public static string GetModeName(ILocalizationService loc, string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return "Unknown";

            if (loc != null)
            {
                var key = $"mode.{modeId}";
                var localized = SafeGet(loc, key);

                if (!string.IsNullOrWhiteSpace(localized) && localized != key)
                    return localized;
            }

            return modeId;
        }

        private static string GetFromGameTable(ILocalizationService loc, string table, string key)
        {
            if (loc == null) return null;
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(key))
                return null;

            var s = loc.GetFromTable(table, key);

            if (string.IsNullOrWhiteSpace(s) || s == key)
                return null;

            return s;
        }

        private static string SafeGet(ILocalizationService loc, string key)
        {
            if (loc == null || string.IsNullOrWhiteSpace(key))
                return key ?? "";

            try
            {
                var s = loc.Get(key);
                return string.IsNullOrWhiteSpace(s) ? key : s;
            }
            catch
            {
                return key;
            }
        }
    }
}