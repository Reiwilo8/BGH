using System;
using System.Collections.Generic;
using System.Globalization;
using Project.Games.Persistence;

namespace Project.Games.Run
{
    public sealed class FishingInitialParametersProvider : IGameInitialParametersProvider
    {
        private const string FishingGameId = "fishing";
        private const string FishingBasePrefix = "fishing.base.";

        private const string KeyFishingBiteWaitMin = FishingBasePrefix + "biteWaitMinSeconds";
        private const string KeyFishingBiteWaitMax = FishingBasePrefix + "biteWaitMaxSeconds";
        private const string KeyFishingReactionWindowBase = FishingBasePrefix + "reactionWindowBaseSeconds";

        private const string KeyFishingCatchDistanceBase = FishingBasePrefix + "catchDistanceBase";
        private const string KeyFishingSpawnDistanceBase = FishingBasePrefix + "spawnDistanceBase";
        private const string KeyFishingSpawnDistanceJitter = FishingBasePrefix + "spawnDistanceJitter";

        private const string KeyFishingTensionMaxTicks = FishingBasePrefix + "tensionMaxTicks";

        private const string KeyFishingActionMinSeconds = FishingBasePrefix + "actionMinSeconds";
        private const string KeyFishingActionMaxSeconds = FishingBasePrefix + "actionMaxSeconds";

        private const string KeyFishingMoveLateralSpeedMin = FishingBasePrefix + "moveLateralSpeedMin";
        private const string KeyFishingMoveLateralSpeedMax = FishingBasePrefix + "moveLateralSpeedMax";

        private const string KeyFishingBurstForwardSpeedMin = FishingBasePrefix + "burstForwardSpeedMin";
        private const string KeyFishingBurstForwardSpeedMax = FishingBasePrefix + "burstForwardSpeedMax";

        private const string KeyFishingFailGraceSeconds = FishingBasePrefix + "failGraceSeconds";

        private const string KeyFishingLoosenDistancePenaltyMin = FishingBasePrefix + "loosenDistancePenaltyMin";
        private const string KeyFishingLoosenDistancePenaltyMax = FishingBasePrefix + "loosenDistancePenaltyMax";

        private const string KeyFishingFatigueGainOnCorrect = FishingBasePrefix + "fatigueGainOnCorrect";
        private const string KeyFishingFatigueLossOnWrong = FishingBasePrefix + "fatigueLossOnWrong";
        private const string KeyFishingFatigueLossOnLoosen = FishingBasePrefix + "fatigueLossOnLoosen";

        private const string ModePrefix = "mode.";
        private const string FishingPerModeMid = ".fishing.";

        private const string KeyFishingModeDifficultyScaleSuffix = "difficultyScale";
        private const string KeyFishingModeTargetFishCountSuffix = "targetFishCount";
        private const string KeyFishingModeAggressionMinSuffix = "aggressionMin";
        private const string KeyFishingModeAggressionMaxSuffix = "aggressionMax";
        private const string KeyFishingModeResistanceMinSuffix = "resistanceMin";
        private const string KeyFishingModeResistanceMaxSuffix = "resistanceMax";
        private const string KeyFishingModeReactionWindowScaleSuffix = "reactionWindowScale";

        private const string FishingCustomPrefix = "custom.fishing.";
        private const string KeyFishingTargetFishCount = FishingCustomPrefix + "targetFishCount";
        private const string KeyFishingDifficultyPreset = FishingCustomPrefix + "difficultyPreset";
        private const string KeyFishingAggressionMax = FishingCustomPrefix + "aggressionMax";

        private const string PBaseBiteWaitMin = "fishing.biteWaitMinSeconds";
        private const string PBaseBiteWaitMax = "fishing.biteWaitMaxSeconds";
        private const string PBaseReactionWindowBase = "fishing.reactionWindowBaseSeconds";

        private const string PCatchDistanceBase = "fishing.catchDistanceBase";
        private const string PSpawnDistanceBase = "fishing.spawnDistanceBase";
        private const string PSpawnDistanceJitter = "fishing.spawnDistanceJitter";

        private const string PTensionMaxTicks = "fishing.tensionMaxTicks";

        private const string PActionMinSeconds = "fishing.actionMinSeconds";
        private const string PActionMaxSeconds = "fishing.actionMaxSeconds";

        private const string PMoveLateralSpeedMin = "fishing.moveLateralSpeedMin";
        private const string PMoveLateralSpeedMax = "fishing.moveLateralSpeedMax";

        private const string PBurstForwardSpeedMin = "fishing.burstForwardSpeedMin";
        private const string PBurstForwardSpeedMax = "fishing.burstForwardSpeedMax";

        private const string PFailGraceSeconds = "fishing.failGraceSeconds";

        private const string PLoosenDistancePenaltyMin = "fishing.loosenDistancePenaltyMin";
        private const string PLoosenDistancePenaltyMax = "fishing.loosenDistancePenaltyMax";

        private const string PFatigueGainOnCorrect = "fishing.fatigueGainOnCorrect";
        private const string PFatigueLossOnWrong = "fishing.fatigueLossOnWrong";
        private const string PFatigueLossOnLoosen = "fishing.fatigueLossOnLoosen";

        private const string PDifficultyScale = "fishing.difficultyScale";
        private const string PTargetFishCount = "fishing.targetFishCount";
        private const string PAggressionMin = "fishing.aggressionMin";
        private const string PAggressionMax = "fishing.aggressionMax";
        private const string PResistanceMin = "fishing.resistanceMin";
        private const string PResistanceMax = "fishing.resistanceMax";
        private const string PReactionWindowScale = "fishing.reactionWindowScale";

        private readonly IGameDataStore _store;

        public FishingInitialParametersProvider(IGameDataStore store)
        {
            _store = store;
        }

        public void AppendParameters(string gameId, string modeId, IDictionary<string, string> initialParameters)
        {
            if (initialParameters == null) return;
            if (string.IsNullOrWhiteSpace(gameId)) return;

            if (!string.Equals(gameId, FishingGameId, StringComparison.OrdinalIgnoreCase))
                return;

            string m = NormalizeModeId(modeId);

            ResolveFishingBaseDefaults(out FishingBaseDefaults baseDef);
            ResolveFishingModeDefaults(m, out FishingModeDefaults modeDef);

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

            bool isCustom = string.Equals(m, "custom", StringComparison.OrdinalIgnoreCase);

            if (isCustom)
            {
                string presetRaw = GetCustomString(g, KeyFishingDifficultyPreset, "medium");
                string preset = NormalizePresetId(presetRaw, fallback: "medium");
                ResolveFishingModeDefaults(preset, out modeDef);

                int target = ParseIntSafe(GetCustomString(g, KeyFishingTargetFishCount, modeDef.targetFishCount.ToString(CultureInfo.InvariantCulture)), modeDef.targetFishCount);
                float aggMax = ParseFloatSafe(GetCustomString(g, KeyFishingAggressionMax, FloatToInv(modeDef.aggressionMax)), modeDef.aggressionMax);

                float aggMin = modeDef.aggressionMin;
                if (aggMin > aggMax) aggMin = aggMax;

                modeDef.targetFishCount = target;
                modeDef.aggressionMax = aggMax;
                modeDef.aggressionMin = aggMin;
            }
            else
            {
                string keyDifficulty = BuildFishingPerModeKey(m, KeyFishingModeDifficultyScaleSuffix);
                string keyTarget = BuildFishingPerModeKey(m, KeyFishingModeTargetFishCountSuffix);
                string keyAggMin = BuildFishingPerModeKey(m, KeyFishingModeAggressionMinSuffix);
                string keyAggMax = BuildFishingPerModeKey(m, KeyFishingModeAggressionMaxSuffix);
                string keyResMin = BuildFishingPerModeKey(m, KeyFishingModeResistanceMinSuffix);
                string keyResMax = BuildFishingPerModeKey(m, KeyFishingModeResistanceMaxSuffix);
                string keyReactionScale = BuildFishingPerModeKey(m, KeyFishingModeReactionWindowScaleSuffix);

                modeDef.difficultyScale = ParseFloatSafe(GetCustomString(g, keyDifficulty, FloatToInv(modeDef.difficultyScale)), modeDef.difficultyScale);
                modeDef.targetFishCount = ParseIntSafe(GetCustomString(g, keyTarget, modeDef.targetFishCount.ToString(CultureInfo.InvariantCulture)), modeDef.targetFishCount);
                modeDef.reactionWindowScale = ParseFloatSafe(GetCustomString(g, keyReactionScale, FloatToInv(modeDef.reactionWindowScale)), modeDef.reactionWindowScale);

                modeDef.aggressionMin = ParseFloatSafe(GetCustomString(g, keyAggMin, FloatToInv(modeDef.aggressionMin)), modeDef.aggressionMin);
                modeDef.aggressionMax = ParseFloatSafe(GetCustomString(g, keyAggMax, FloatToInv(modeDef.aggressionMax)), modeDef.aggressionMax);

                modeDef.resistanceMin = ParseFloatSafe(GetCustomString(g, keyResMin, FloatToInv(modeDef.resistanceMin)), modeDef.resistanceMin);
                modeDef.resistanceMax = ParseFloatSafe(GetCustomString(g, keyResMax, FloatToInv(modeDef.resistanceMax)), modeDef.resistanceMax);
            }

            if (modeDef.aggressionMin > modeDef.aggressionMax) modeDef.aggressionMin = modeDef.aggressionMax;
            if (modeDef.resistanceMin > modeDef.resistanceMax) modeDef.resistanceMin = modeDef.resistanceMax;

            string biteMin = GetCustomString(g, KeyFishingBiteWaitMin, FloatToInv(baseDef.biteWaitMinSeconds));
            string biteMax = GetCustomString(g, KeyFishingBiteWaitMax, FloatToInv(baseDef.biteWaitMaxSeconds));
            string reactBase = GetCustomString(g, KeyFishingReactionWindowBase, FloatToInv(baseDef.reactionWindowBaseSeconds));

            string catchDist = GetCustomString(g, KeyFishingCatchDistanceBase, FloatToInv(baseDef.catchDistanceBase));
            string spawnBase = GetCustomString(g, KeyFishingSpawnDistanceBase, FloatToInv(baseDef.spawnDistanceBase));
            string spawnJit = GetCustomString(g, KeyFishingSpawnDistanceJitter, FloatToInv(baseDef.spawnDistanceJitter));

            string tensionMaxTicks = GetCustomString(g, KeyFishingTensionMaxTicks, baseDef.tensionMaxTicks.ToString(CultureInfo.InvariantCulture));

            string actionMin = GetCustomString(g, KeyFishingActionMinSeconds, FloatToInv(baseDef.actionMinSeconds));
            string actionMax = GetCustomString(g, KeyFishingActionMaxSeconds, FloatToInv(baseDef.actionMaxSeconds));

            string moveMin = GetCustomString(g, KeyFishingMoveLateralSpeedMin, FloatToInv(baseDef.moveLateralSpeedMin));
            string moveMax = GetCustomString(g, KeyFishingMoveLateralSpeedMax, FloatToInv(baseDef.moveLateralSpeedMax));

            string burstMin = GetCustomString(g, KeyFishingBurstForwardSpeedMin, FloatToInv(baseDef.burstForwardSpeedMin));
            string burstMax = GetCustomString(g, KeyFishingBurstForwardSpeedMax, FloatToInv(baseDef.burstForwardSpeedMax));

            string failGrace = GetCustomString(g, KeyFishingFailGraceSeconds, FloatToInv(baseDef.failGraceSeconds));

            string loosenMin = GetCustomString(g, KeyFishingLoosenDistancePenaltyMin, FloatToInv(baseDef.loosenDistancePenaltyMin));
            string loosenMax = GetCustomString(g, KeyFishingLoosenDistancePenaltyMax, FloatToInv(baseDef.loosenDistancePenaltyMax));

            string fatGain = GetCustomString(g, KeyFishingFatigueGainOnCorrect, FloatToInv(baseDef.fatigueGainOnCorrect));
            string fatWrong = GetCustomString(g, KeyFishingFatigueLossOnWrong, FloatToInv(baseDef.fatigueLossOnWrong));
            string fatLoosen = GetCustomString(g, KeyFishingFatigueLossOnLoosen, FloatToInv(baseDef.fatigueLossOnLoosen));

            initialParameters[PBaseBiteWaitMin] = biteMin;
            initialParameters[PBaseBiteWaitMax] = biteMax;
            initialParameters[PBaseReactionWindowBase] = reactBase;

            initialParameters[PCatchDistanceBase] = catchDist;
            initialParameters[PSpawnDistanceBase] = spawnBase;
            initialParameters[PSpawnDistanceJitter] = spawnJit;

            initialParameters[PTensionMaxTicks] = tensionMaxTicks;

            initialParameters[PActionMinSeconds] = actionMin;
            initialParameters[PActionMaxSeconds] = actionMax;

            initialParameters[PMoveLateralSpeedMin] = moveMin;
            initialParameters[PMoveLateralSpeedMax] = moveMax;

            initialParameters[PBurstForwardSpeedMin] = burstMin;
            initialParameters[PBurstForwardSpeedMax] = burstMax;

            initialParameters[PFailGraceSeconds] = failGrace;

            initialParameters[PLoosenDistancePenaltyMin] = loosenMin;
            initialParameters[PLoosenDistancePenaltyMax] = loosenMax;

            initialParameters[PFatigueGainOnCorrect] = fatGain;
            initialParameters[PFatigueLossOnWrong] = fatWrong;
            initialParameters[PFatigueLossOnLoosen] = fatLoosen;

            initialParameters[PDifficultyScale] = FloatToInv(modeDef.difficultyScale);
            initialParameters[PTargetFishCount] = modeDef.targetFishCount.ToString(CultureInfo.InvariantCulture);
            initialParameters[PReactionWindowScale] = FloatToInv(modeDef.reactionWindowScale);

            initialParameters[PAggressionMin] = FloatToInv(modeDef.aggressionMin);
            initialParameters[PAggressionMax] = FloatToInv(modeDef.aggressionMax);

            initialParameters[PResistanceMin] = FloatToInv(modeDef.resistanceMin);
            initialParameters[PResistanceMax] = FloatToInv(modeDef.resistanceMax);
        }

        private static string NormalizeModeId(string modeId)
            => string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();

        private static string BuildFishingPerModeKey(string modeId, string suffix)
            => ModePrefix + modeId + FishingPerModeMid + suffix;

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
            ResolveFishingBaseDefaults(out FishingBaseDefaults baseDef);
            ResolveFishingModeDefaults(modeId, out FishingModeDefaults modeDef);

            dict[PBaseBiteWaitMin] = FloatToInv(baseDef.biteWaitMinSeconds);
            dict[PBaseBiteWaitMax] = FloatToInv(baseDef.biteWaitMaxSeconds);
            dict[PBaseReactionWindowBase] = FloatToInv(baseDef.reactionWindowBaseSeconds);

            dict[PCatchDistanceBase] = FloatToInv(baseDef.catchDistanceBase);
            dict[PSpawnDistanceBase] = FloatToInv(baseDef.spawnDistanceBase);
            dict[PSpawnDistanceJitter] = FloatToInv(baseDef.spawnDistanceJitter);

            dict[PTensionMaxTicks] = baseDef.tensionMaxTicks.ToString(CultureInfo.InvariantCulture);

            dict[PActionMinSeconds] = FloatToInv(baseDef.actionMinSeconds);
            dict[PActionMaxSeconds] = FloatToInv(baseDef.actionMaxSeconds);

            dict[PMoveLateralSpeedMin] = FloatToInv(baseDef.moveLateralSpeedMin);
            dict[PMoveLateralSpeedMax] = FloatToInv(baseDef.moveLateralSpeedMax);

            dict[PBurstForwardSpeedMin] = FloatToInv(baseDef.burstForwardSpeedMin);
            dict[PBurstForwardSpeedMax] = FloatToInv(baseDef.burstForwardSpeedMax);

            dict[PFailGraceSeconds] = FloatToInv(baseDef.failGraceSeconds);

            dict[PLoosenDistancePenaltyMin] = FloatToInv(baseDef.loosenDistancePenaltyMin);
            dict[PLoosenDistancePenaltyMax] = FloatToInv(baseDef.loosenDistancePenaltyMax);

            dict[PFatigueGainOnCorrect] = FloatToInv(baseDef.fatigueGainOnCorrect);
            dict[PFatigueLossOnWrong] = FloatToInv(baseDef.fatigueLossOnWrong);
            dict[PFatigueLossOnLoosen] = FloatToInv(baseDef.fatigueLossOnLoosen);

            dict[PDifficultyScale] = FloatToInv(modeDef.difficultyScale);
            dict[PTargetFishCount] = modeDef.targetFishCount.ToString(CultureInfo.InvariantCulture);
            dict[PReactionWindowScale] = FloatToInv(modeDef.reactionWindowScale);

            dict[PAggressionMin] = FloatToInv(modeDef.aggressionMin);
            dict[PAggressionMax] = FloatToInv(modeDef.aggressionMax);

            dict[PResistanceMin] = FloatToInv(modeDef.resistanceMin);
            dict[PResistanceMax] = FloatToInv(modeDef.resistanceMax);
        }

        private static string NormalizePresetId(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback ?? "medium";

            string s = raw.Trim();

            if (string.Equals(s, "tutorial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "samouczek", StringComparison.OrdinalIgnoreCase))
                return "tutorial";

            if (string.Equals(s, "easy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "latwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "³atwy", StringComparison.OrdinalIgnoreCase))
                return "easy";

            if (string.Equals(s, "medium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "sredni", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "œredni", StringComparison.OrdinalIgnoreCase))
                return "medium";

            if (string.Equals(s, "hard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "trudny", StringComparison.OrdinalIgnoreCase))
                return "hard";

            return fallback ?? "medium";
        }

        private static void ResolveFishingBaseDefaults(out FishingBaseDefaults d)
        {
            d = new FishingBaseDefaults
            {
                biteWaitMinSeconds = 2.0f,
                biteWaitMaxSeconds = 6.0f,
                reactionWindowBaseSeconds = 1.25f,

                catchDistanceBase = 0.075f,
                spawnDistanceBase = 0.62f,
                spawnDistanceJitter = 0.16f,

                tensionMaxTicks = 4,

                actionMinSeconds = 1.30f,
                actionMaxSeconds = 2.60f,

                moveLateralSpeedMin = 0.16f,
                moveLateralSpeedMax = 0.34f,

                burstForwardSpeedMin = 0.42f,
                burstForwardSpeedMax = 0.80f,

                failGraceSeconds = 0.28f,

                loosenDistancePenaltyMin = 0.015f,
                loosenDistancePenaltyMax = 0.045f,

                fatigueGainOnCorrect = 0.10f,
                fatigueLossOnWrong = 0.18f,
                fatigueLossOnLoosen = 0.35f
            };
        }

        private static void ResolveFishingModeDefaults(string modeId, out FishingModeDefaults d)
        {
            string id = string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();

            if (string.Equals(id, "tutorial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "samouczek", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 0.50f,
                    targetFishCount = 1,
                    aggressionMin = 0.00f,
                    aggressionMax = 0.20f,
                    resistanceMin = 0.70f,
                    resistanceMax = 0.95f,
                    reactionWindowScale = 1.30f
                };
                return;
            }

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "latwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "³atwy", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 0.80f,
                    targetFishCount = 3,
                    aggressionMin = 0.10f,
                    aggressionMax = 0.55f,
                    resistanceMin = 0.90f,
                    resistanceMax = 1.25f,
                    reactionWindowScale = 1.10f
                };
                return;
            }

            if (string.Equals(id, "medium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "sredni", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "œredni", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 1.10f,
                    targetFishCount = 5,
                    aggressionMin = 0.25f,
                    aggressionMax = 0.80f,
                    resistanceMin = 1.05f,
                    resistanceMax = 1.55f,
                    reactionWindowScale = 1.00f
                };
                return;
            }

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "trudny", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 1.35f,
                    targetFishCount = 7,
                    aggressionMin = 0.55f,
                    aggressionMax = 0.98f,
                    resistanceMin = 1.20f,
                    resistanceMax = 1.90f,
                    reactionWindowScale = 0.92f
                };
                return;
            }

            d = new FishingModeDefaults
            {
                difficultyScale = 1.10f,
                targetFishCount = 5,
                aggressionMin = 0.25f,
                aggressionMax = 0.80f,
                resistanceMin = 1.05f,
                resistanceMax = 1.55f,
                reactionWindowScale = 1.00f
            };
        }

        private static float ParseFloatSafe(string s, float fallback)
        {
            if (string.IsNullOrWhiteSpace(s))
                return fallback;

            try
            {
                if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                    return fallback;

                if (float.IsNaN(v) || float.IsInfinity(v))
                    return fallback;

                return v;
            }
            catch
            {
                return fallback;
            }
        }

        private static int ParseIntSafe(string s, int fallback)
        {
            if (string.IsNullOrWhiteSpace(s))
                return fallback;

            try
            {
                if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                    return fallback;

                return v;
            }
            catch
            {
                return fallback;
            }
        }

        private static string FloatToInv(float v)
            => v.ToString("0.###", CultureInfo.InvariantCulture);

        private struct FishingBaseDefaults
        {
            public float biteWaitMinSeconds;
            public float biteWaitMaxSeconds;
            public float reactionWindowBaseSeconds;

            public float catchDistanceBase;
            public float spawnDistanceBase;
            public float spawnDistanceJitter;

            public int tensionMaxTicks;

            public float actionMinSeconds;
            public float actionMaxSeconds;

            public float moveLateralSpeedMin;
            public float moveLateralSpeedMax;

            public float burstForwardSpeedMin;
            public float burstForwardSpeedMax;

            public float failGraceSeconds;

            public float loosenDistancePenaltyMin;
            public float loosenDistancePenaltyMax;

            public float fatigueGainOnCorrect;
            public float fatigueLossOnWrong;
            public float fatigueLossOnLoosen;
        }

        private struct FishingModeDefaults
        {
            public float difficultyScale;
            public int targetFishCount;

            public float aggressionMin;
            public float aggressionMax;

            public float resistanceMin;
            public float resistanceMax;

            public float reactionWindowScale;
        }
    }
}