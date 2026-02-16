using System;
using System.Collections.Generic;
using System.Globalization;
using Project.Games.Persistence;

namespace Project.Games.Run
{
    public sealed class SteamRushInitialParametersProvider : IGameInitialParametersProvider
    {
        private const string SteamRushGameId = "steamrush";

        private const string SteamRushBasePrefix = "steamrush.base.";

        private const string KeyApproachSeconds = SteamRushBasePrefix + "approachSeconds";
        private const string KeyPassSeconds = SteamRushBasePrefix + "passSeconds";
        private const string KeySpawnRateScale = SteamRushBasePrefix + "spawnRateScale";

        private const string SteamRushPerModePrefix = "mode.";
        private const string SteamRushPerModeMid = ".steamrush.";

        private const string KeyDifficultyScaleSuffix = "difficultyScale";
        private const string KeyPatternTierMaxSuffix = "patternTierMax";
        private const string KeyModeDurationSecondsSuffix = "modeDurationSeconds";
        private const string KeyWhistleMomentSuffix = "whistleMoment";

        private const string PApproachSeconds = "steamrush.approachSeconds";
        private const string PPassSeconds = "steamrush.passSeconds";
        private const string PSpawnRateScale = "steamrush.spawnRateScale";

        private const string PDifficultyScale = "steamrush.difficultyScale";
        private const string PPatternTierMax = "steamrush.patternTierMax";
        private const string PModeDurationSeconds = "steamrush.modeDurationSeconds";
        private const string PWhistleMoment = "steamrush.whistleMoment";

        private readonly IGameDataStore _store;

        public SteamRushInitialParametersProvider(IGameDataStore store)
        {
            _store = store;
        }

        public void AppendParameters(string gameId, string modeId, IDictionary<string, string> initialParameters)
        {
            if (initialParameters == null) return;
            if (string.IsNullOrWhiteSpace(gameId)) return;

            if (!string.Equals(gameId, SteamRushGameId, StringComparison.OrdinalIgnoreCase))
                return;

            string m = NormalizeModeId(modeId);

            if (_store == null)
            {
                AppendDefaults(m, initialParameters);
                return;
            }

            var g = SafeGetOrCreateGame(gameId);
            if (g == null)
            {
                AppendDefaults(m, initialParameters);
                return;
            }

            const float defApproach = 3.10f;
            const float defPass = 0.55f;
            const float defRateScale = 1.00f;

            float defDifficulty = ResolveSteamRushDefaultDifficultyScale(m);
            int defPatternTierMax = ResolveSteamRushDefaultPatternTierMax(m);
            float defDuration = ResolveSteamRushDefaultDurationSeconds(m);

            const float defWhistleMoment = 0.50f;

            string keyDifficulty = BuildSteamRushPerModeKey(m, KeyDifficultyScaleSuffix);
            string keyPatternTierMax = BuildSteamRushPerModeKey(m, KeyPatternTierMaxSuffix);
            string keyDuration = BuildSteamRushPerModeKey(m, KeyModeDurationSecondsSuffix);
            string keyWhistle = BuildSteamRushPerModeKey(m, KeyWhistleMomentSuffix);

            string approach = GetCustomString(g, KeyApproachSeconds, FloatToInv(defApproach));
            string pass = GetCustomString(g, KeyPassSeconds, FloatToInv(defPass));
            string rateScale = GetCustomString(g, KeySpawnRateScale, FloatToInv(defRateScale));

            string difficulty = GetCustomString(g, keyDifficulty, FloatToInv(defDifficulty));
            string tierMax = GetCustomString(g, keyPatternTierMax, IntToInv(defPatternTierMax));

            string duration = GetCustomString(g, keyDuration, FloatToInv(defDuration));
            string whistle = GetCustomString(g, keyWhistle, FloatToInv(defWhistleMoment));

            initialParameters[PApproachSeconds] = approach;
            initialParameters[PPassSeconds] = pass;
            initialParameters[PSpawnRateScale] = rateScale;

            initialParameters[PDifficultyScale] = difficulty;
            initialParameters[PPatternTierMax] = tierMax;
            initialParameters[PModeDurationSeconds] = duration;
            initialParameters[PWhistleMoment] = whistle;
        }

        private static string NormalizeModeId(string modeId)
        {
            return string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();
        }

        private static string BuildSteamRushPerModeKey(string modeId, string suffix)
        {
            return SteamRushPerModePrefix + modeId + SteamRushPerModeMid + suffix;
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

                try
                {
                    var decoded = PersistenceValueCodec.DecodePossiblyJsonString(raw);
                    return string.IsNullOrWhiteSpace(decoded) ? defaultValue : decoded;
                }
                catch
                {
                    return raw;
                }
            }

            return defaultValue;
        }

        private static void AppendDefaults(string modeId, IDictionary<string, string> dict)
        {
            dict[PApproachSeconds] = FloatToInv(3.10f);
            dict[PPassSeconds] = FloatToInv(0.55f);
            dict[PSpawnRateScale] = FloatToInv(1.00f);

            dict[PDifficultyScale] = FloatToInv(ResolveSteamRushDefaultDifficultyScale(modeId));
            dict[PPatternTierMax] = IntToInv(ResolveSteamRushDefaultPatternTierMax(modeId));
            dict[PModeDurationSeconds] = FloatToInv(ResolveSteamRushDefaultDurationSeconds(modeId));

            dict[PWhistleMoment] = FloatToInv(0.50f);
        }

        private static bool IsEndlessMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId)) return false;
            var id = modeId.Trim();

            return string.Equals(id, "endless", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "nieskonczony", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "nieskoñczony", StringComparison.OrdinalIgnoreCase);
        }

        private static float ResolveSteamRushDefaultDifficultyScale(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return 1.00f;

            var id = modeId.Trim();

            if (string.Equals(id, "tutorial", StringComparison.OrdinalIgnoreCase))
                return 0.50f;

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase))
                return 0.7f;

            if (string.Equals(id, "medium", StringComparison.OrdinalIgnoreCase))
                return 1.00f;

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase))
                return 1.30f;

            if (IsEndlessMode(id))
                return 1.00f;

            return 1.00f;
        }

        private static int ResolveSteamRushDefaultPatternTierMax(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return 2;

            var id = modeId.Trim();

            if (string.Equals(id, "tutorial", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(id, "medium", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase))
                return 3;

            if (IsEndlessMode(id))
                return 3;

            return 2;
        }

        private static float ResolveSteamRushDefaultDurationSeconds(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return 60f;

            var id = modeId.Trim();

            if (string.Equals(id, "tutorial", StringComparison.OrdinalIgnoreCase))
                return 30f;

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase))
                return 60f;

            if (string.Equals(id, "medium", StringComparison.OrdinalIgnoreCase))
                return 180f;

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase))
                return 300f;

            return 60f;
        }

        private static string FloatToInv(float v)
            => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static string IntToInv(int v)
            => v.ToString(CultureInfo.InvariantCulture);
    }
}