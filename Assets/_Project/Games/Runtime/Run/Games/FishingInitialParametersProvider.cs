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
        private const string KeyFishingDifficultySpawnBias = FishingBasePrefix + "difficultySpawnBias";

        private const string KeyFishingTensionDecayIdle = FishingBasePrefix + "tensionDecayIdle";
        private const string KeyFishingTensionDecayUp = FishingBasePrefix + "tensionDecayUp";
        private const string KeyFishingTensionReelFactor = FishingBasePrefix + "tensionReelFactor";

        private const string KeyFishingMoveChanceBase = FishingBasePrefix + "moveChanceBase";
        private const string KeyFishingBurstChanceBase = FishingBasePrefix + "burstChanceBase";

        private const string KeyFishingIdleSpeed = FishingBasePrefix + "idleSpeed";
        private const string KeyFishingNormalSpeed = FishingBasePrefix + "normalSpeed";
        private const string KeyFishingBurstSpeed = FishingBasePrefix + "burstSpeed";

        private const string KeyFishingFatigueGainFromSpeed = FishingBasePrefix + "fatigueGainFromSpeed";
        private const string KeyFishingFatigueGainFromFight = FishingBasePrefix + "fatigueGainFromFight";
        private const string KeyFishingFatigueRecoveryIdle = FishingBasePrefix + "fatigueRecoveryIdle";
        private const string KeyFishingBurstFatigueCutoff = FishingBasePrefix + "burstFatigueCutoff";
        private const string KeyFishingFatigueSurrenderThreshold = FishingBasePrefix + "fatigueSurrenderThreshold";

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
        private const string PDifficultySpawnBias = "fishing.difficultySpawnBias";

        private const string PTensionDecayIdle = "fishing.tensionDecayIdle";
        private const string PTensionDecayUp = "fishing.tensionDecayUp";
        private const string PTensionReelFactor = "fishing.tensionReelFactor";

        private const string PMoveChanceBase = "fishing.moveChanceBase";
        private const string PBurstChanceBase = "fishing.burstChanceBase";

        private const string PIdleSpeed = "fishing.idleSpeed";
        private const string PNormalSpeed = "fishing.normalSpeed";
        private const string PBurstSpeed = "fishing.burstSpeed";

        private const string PFatigueGainFromSpeed = "fishing.fatigueGainFromSpeed";
        private const string PFatigueGainFromFight = "fishing.fatigueGainFromFight";
        private const string PFatigueRecoveryIdle = "fishing.fatigueRecoveryIdle";
        private const string PBurstFatigueCutoff = "fishing.burstFatigueCutoff";
        private const string PFatigueSurrenderThreshold = "fishing.fatigueSurrenderThreshold";

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

            ResolveFishingBaseDefaults(out FishingBaseDefaults baseDef);

            FishingModeDefaults modeDef;
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
                ResolveFishingModeDefaults(m, out modeDef);
            }

            string biteMin = GetCustomString(g, KeyFishingBiteWaitMin, FloatToInv(baseDef.biteWaitMinSeconds));
            string biteMax = GetCustomString(g, KeyFishingBiteWaitMax, FloatToInv(baseDef.biteWaitMaxSeconds));
            string reactBase = GetCustomString(g, KeyFishingReactionWindowBase, FloatToInv(baseDef.reactionWindowBaseSeconds));

            string catchDist = GetCustomString(g, KeyFishingCatchDistanceBase, FloatToInv(baseDef.catchDistanceBase));
            string spawnBase = GetCustomString(g, KeyFishingSpawnDistanceBase, FloatToInv(baseDef.spawnDistanceBase));
            string spawnJit = GetCustomString(g, KeyFishingSpawnDistanceJitter, FloatToInv(baseDef.spawnDistanceJitter));
            string spawnBias = GetCustomString(g, KeyFishingDifficultySpawnBias, FloatToInv(baseDef.difficultySpawnBias));

            string tIdle = GetCustomString(g, KeyFishingTensionDecayIdle, FloatToInv(baseDef.tensionDecayIdle));
            string tUp = GetCustomString(g, KeyFishingTensionDecayUp, FloatToInv(baseDef.tensionDecayUp));
            string tReel = GetCustomString(g, KeyFishingTensionReelFactor, FloatToInv(baseDef.tensionReelFactor));

            string moveChance = GetCustomString(g, KeyFishingMoveChanceBase, FloatToInv(baseDef.moveChanceBase));
            string burstChance = GetCustomString(g, KeyFishingBurstChanceBase, FloatToInv(baseDef.burstChanceBase));

            string idleSpeed = GetCustomString(g, KeyFishingIdleSpeed, FloatToInv(baseDef.idleSpeed));
            string normalSpeed = GetCustomString(g, KeyFishingNormalSpeed, FloatToInv(baseDef.normalSpeed));
            string burstSpeed = GetCustomString(g, KeyFishingBurstSpeed, FloatToInv(baseDef.burstSpeed));

            string fatSpd = GetCustomString(g, KeyFishingFatigueGainFromSpeed, FloatToInv(baseDef.fatigueGainFromSpeed));
            string fatFight = GetCustomString(g, KeyFishingFatigueGainFromFight, FloatToInv(baseDef.fatigueGainFromFight));
            string fatRec = GetCustomString(g, KeyFishingFatigueRecoveryIdle, FloatToInv(baseDef.fatigueRecoveryIdle));
            string fatCut = GetCustomString(g, KeyFishingBurstFatigueCutoff, FloatToInv(baseDef.burstFatigueCutoff));
            string fatSurr = GetCustomString(g, KeyFishingFatigueSurrenderThreshold, FloatToInv(baseDef.fatigueSurrenderThreshold));

            if (!isCustom)
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

            initialParameters[PBaseBiteWaitMin] = biteMin;
            initialParameters[PBaseBiteWaitMax] = biteMax;
            initialParameters[PBaseReactionWindowBase] = reactBase;

            initialParameters[PCatchDistanceBase] = catchDist;
            initialParameters[PSpawnDistanceBase] = spawnBase;
            initialParameters[PSpawnDistanceJitter] = spawnJit;
            initialParameters[PDifficultySpawnBias] = spawnBias;

            initialParameters[PTensionDecayIdle] = tIdle;
            initialParameters[PTensionDecayUp] = tUp;
            initialParameters[PTensionReelFactor] = tReel;

            initialParameters[PMoveChanceBase] = moveChance;
            initialParameters[PBurstChanceBase] = burstChance;

            initialParameters[PIdleSpeed] = idleSpeed;
            initialParameters[PNormalSpeed] = normalSpeed;
            initialParameters[PBurstSpeed] = burstSpeed;

            initialParameters[PFatigueGainFromSpeed] = fatSpd;
            initialParameters[PFatigueGainFromFight] = fatFight;
            initialParameters[PFatigueRecoveryIdle] = fatRec;
            initialParameters[PBurstFatigueCutoff] = fatCut;
            initialParameters[PFatigueSurrenderThreshold] = fatSurr;

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
            dict[PDifficultySpawnBias] = FloatToInv(baseDef.difficultySpawnBias);

            dict[PTensionDecayIdle] = FloatToInv(baseDef.tensionDecayIdle);
            dict[PTensionDecayUp] = FloatToInv(baseDef.tensionDecayUp);
            dict[PTensionReelFactor] = FloatToInv(baseDef.tensionReelFactor);

            dict[PMoveChanceBase] = FloatToInv(baseDef.moveChanceBase);
            dict[PBurstChanceBase] = FloatToInv(baseDef.burstChanceBase);

            dict[PIdleSpeed] = FloatToInv(baseDef.idleSpeed);
            dict[PNormalSpeed] = FloatToInv(baseDef.normalSpeed);
            dict[PBurstSpeed] = FloatToInv(baseDef.burstSpeed);

            dict[PFatigueGainFromSpeed] = FloatToInv(baseDef.fatigueGainFromSpeed);
            dict[PFatigueGainFromFight] = FloatToInv(baseDef.fatigueGainFromFight);
            dict[PFatigueRecoveryIdle] = FloatToInv(baseDef.fatigueRecoveryIdle);
            dict[PBurstFatigueCutoff] = FloatToInv(baseDef.burstFatigueCutoff);
            dict[PFatigueSurrenderThreshold] = FloatToInv(baseDef.fatigueSurrenderThreshold);

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

                catchDistanceBase = 0.08f,
                spawnDistanceBase = 0.60f,
                spawnDistanceJitter = 0.15f,
                difficultySpawnBias = 0.03f,

                tensionDecayIdle = 0.06f,
                tensionDecayUp = 0.35f,
                tensionReelFactor = 0.30f,

                moveChanceBase = 0.55f,
                burstChanceBase = 0.12f,

                idleSpeed = 0.00f,
                normalSpeed = 0.22f,
                burstSpeed = 0.45f,

                fatigueGainFromSpeed = 0.10f,
                fatigueGainFromFight = 0.18f,
                fatigueRecoveryIdle = 0.12f,
                burstFatigueCutoff = 0.65f,
                fatigueSurrenderThreshold = 0.85f
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
                    aggressionMax = 0.30f,
                    resistanceMin = 0.75f,
                    resistanceMax = 1.00f,
                    reactionWindowScale = 1.25f
                };
                return;
            }

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "latwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "³atwy", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 0.70f,
                    targetFishCount = 3,
                    aggressionMin = 0.10f,
                    aggressionMax = 0.50f,
                    resistanceMin = 0.85f,
                    resistanceMax = 1.20f,
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
                    difficultyScale = 1.00f,
                    targetFishCount = 5,
                    aggressionMin = 0.20f,
                    aggressionMax = 0.70f,
                    resistanceMin = 0.95f,
                    resistanceMax = 1.40f,
                    reactionWindowScale = 1.00f
                };
                return;
            }

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "trudny", StringComparison.OrdinalIgnoreCase))
            {
                d = new FishingModeDefaults
                {
                    difficultyScale = 1.30f,
                    targetFishCount = 7,
                    aggressionMin = 0.40f,
                    aggressionMax = 0.90f,
                    resistanceMin = 1.10f,
                    resistanceMax = 1.70f,
                    reactionWindowScale = 0.90f
                };
                return;
            }

            d = new FishingModeDefaults
            {
                difficultyScale = 1.00f,
                targetFishCount = 5,
                aggressionMin = 0.20f,
                aggressionMax = 0.70f,
                resistanceMin = 0.95f,
                resistanceMax = 1.40f,
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
            public float difficultySpawnBias;

            public float tensionDecayIdle;
            public float tensionDecayUp;
            public float tensionReelFactor;

            public float moveChanceBase;
            public float burstChanceBase;

            public float idleSpeed;
            public float normalSpeed;
            public float burstSpeed;

            public float fatigueGainFromSpeed;
            public float fatigueGainFromFight;
            public float fatigueRecoveryIdle;
            public float burstFatigueCutoff;
            public float fatigueSurrenderThreshold;
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