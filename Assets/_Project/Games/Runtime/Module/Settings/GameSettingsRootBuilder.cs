using System;
using System.Collections.Generic;
using System.Globalization;
using Project.Core.App;
using Project.Core.Settings.Ui;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Games.Persistence;
using Project.Games.Run;
using Project.Games.Stats;
using UnityEngine;

namespace Project.Games.Module.Settings
{
    public sealed class GameSettingsRootBuilder : ISettingsRootBuilder
    {
        private const string SteamRushGameId = "steamrush";
        private const string FishingGameId = "fishing";
        private const string MemoryGameId = "memory";

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

        private readonly IGameStatsPreferencesService _prefs;
        private readonly IGameStatsService _stats;

        private readonly AppSession _session;
        private readonly GameCatalog _catalog;

        private readonly IGameDataStore _store;

        private readonly IGameRunParametersService _runParams;

        public GameSettingsRootBuilder()
        {
            var services = Core.App.AppContext.Services;

            _session = services.Resolve<AppSession>();

            _prefs = services.Resolve<IGameStatsPreferencesService>();
            _stats = services.Resolve<IGameStatsService>();

            try { _catalog = services.Resolve<GameCatalog>(); }
            catch { _catalog = null; }

            try { _store = services.Resolve<IGameDataStore>(); }
            catch { _store = null; }

            try { _runParams = services.Resolve<IGameRunParametersService>(); }
            catch { _runParams = null; }
        }

        public List<SettingsItem> BuildRoot(SettingsBuildContext context)
        {
            var root = new List<SettingsItem>();

            string gameId = _session != null ? _session.SelectedGameId : null;
            var game = ResolveSelectedGame(gameId);

            bool dev = IsDeveloperMode(context);

            if (dev)
                root.Add(BuildSeedFolder(gameId));

            if (dev && string.Equals(gameId, SteamRushGameId, StringComparison.OrdinalIgnoreCase))
                root.Add(BuildSteamRushBaseFolder(gameId));

            if (dev && string.Equals(gameId, FishingGameId, StringComparison.OrdinalIgnoreCase))
                root.Add(BuildFishingBaseFolder(gameId));

            if (dev)
            {
                var modesFolder = BuildModesFolderIfAny(gameId, game);
                if (modesFolder != null)
                    root.Add(modesFolder);
            }

            var customFolder = BuildCustomFolderIfAny(gameId, game);
            if (customFolder != null)
                root.Add(customFolder);

            root.Add(BuildStatsFolder());

            root.Add(BuildResetSettingsAction(gameId, includeDeveloper: dev));

            root.Add(new SettingsAction("common.back", execute: () => { }));

            return root;
        }

        private GameDefinition ResolveSelectedGame(string gameId)
        {
            if (_catalog == null || string.IsNullOrWhiteSpace(gameId))
                return null;

            try { return _catalog.GetById(gameId); }
            catch { return null; }
        }

        private static bool IsDeveloperMode(SettingsBuildContext context)
        {
            try { return context.DeveloperMode; }
            catch { return false; }
        }

        private SettingsFolder BuildSeedFolder(string gameId)
        {
            return new SettingsFolder(
                labelKey: "settings.run_params",
                descriptionKey: "settings.run_params.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildUseRandomSeedToggle(gameId),
                    BuildKnownSeedsList(gameId),
                    BuildResetSeedHistoryAction(gameId),
                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsToggle BuildUseRandomSeedToggle(string gameId)
        {
            return new SettingsToggle(
                labelKey: "settings.run_params.random_seed",
                descriptionKey: "settings.run_params.random_seed.desc",
                getValue: () =>
                {
                    if (_runParams == null) return true;
                    return _runParams.GetUseRandomSeed(gameId);
                },
                setValue: v =>
                {
                    _runParams?.SetUseRandomSeed(gameId, v);
                }
            );
        }

        private SettingsList BuildKnownSeedsList(string gameId)
        {
            return new SettingsList(
                labelKey: "settings.run_params.seed_list",
                descriptionKey: "settings.run_params.seed_list.desc",
                getOptions: () =>
                {
                    var opts = new List<SettingsListOption>();

                    if (_runParams == null)
                    {
                        opts.Add(new SettingsListOption(id: "none", labelKey: "settings.run_params.seed.none"));
                        return opts;
                    }

                    var seeds = _runParams.GetKnownSeeds(gameId);
                    if (seeds == null || seeds.Count == 0)
                    {
                        opts.Add(new SettingsListOption(id: "none", labelKey: "settings.run_params.seed.none"));
                        return opts;
                    }

                    for (int i = 0; i < seeds.Count; i++)
                    {
                        string id = seeds[i].ToString();
                        opts.Add(new SettingsListOption(id: id, labelKey: id));
                    }

                    return opts;
                },
                getIndex: () =>
                {
                    if (_runParams == null)
                        return 0;

                    var seeds = _runParams.GetKnownSeeds(gameId);
                    if (seeds == null || seeds.Count == 0)
                        return 0;

                    if (_runParams.TryGetSelectedSeed(gameId, out int selected))
                    {
                        for (int i = 0; i < seeds.Count; i++)
                            if (seeds[i] == selected)
                                return i;
                    }

                    return 0;
                },
                setIndex: idx =>
                {
                    if (_runParams == null)
                        return;

                    var seeds = _runParams.GetKnownSeeds(gameId);
                    if (seeds == null || seeds.Count == 0)
                        return;

                    if (idx < 0) idx = 0;
                    if (idx >= seeds.Count) idx = seeds.Count - 1;

                    _runParams.SetSelectedSeed(gameId, seeds[idx]);
                }
            );
        }

        private SettingsAction BuildResetSeedHistoryAction(string gameId)
        {
            return new SettingsAction(
                labelKey: "settings.run_params.seed_history.reset",
                descriptionKey: "settings.run_params.seed_history.reset.desc",
                execute: () =>
                {
                    ResetSeedHistory(gameId);
                }
            );
        }

        private void ResetSeedHistory(string gameId)
        {
            var g = GetOrCreateGameEntry(gameId);
            if (g == null || g.prefs == null || g.prefs.knownSeeds == null)
                return;

            g.prefs.knownSeeds.Clear();
            SaveStoreSafe();
        }

        private SettingsFolder BuildSteamRushBaseFolder(string gameId)
        {
            const float defApproach = 3.10f;
            const float defPass = 0.55f;
            const float defRateScale = 1.00f;

            return new SettingsFolder(
                labelKey: "settings.base",
                descriptionKey: "settings.base.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildFloatRangeSetting(
                        labelKey: "settings.base.approach",
                        descriptionKey: "settings.base.approach.desc",
                        customKey: KeyApproachSeconds,
                        min: 0.25f, max: 5.0f, step: 0.05f,
                        defaultValue: defApproach),

                    BuildFloatRangeSetting(
                        labelKey: "settings.base.pass",
                        descriptionKey: "settings.base.pass.desc",
                        customKey: KeyPassSeconds,
                        min: 0.10f, max: 5.0f, step: 0.05f,
                        defaultValue: defPass),

                    BuildFloatRangeSetting(
                        labelKey: "settings.base.rate_scale",
                        descriptionKey: "settings.base.rate_scale.desc",
                        customKey: KeySpawnRateScale,
                        min: 0.20f, max: 3.00f, step: 0.05f,
                        defaultValue: defRateScale),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsFolder BuildFishingBaseFolder(string gameId)
        {
            const float defBiteWaitMin = 2.0f;
            const float defBiteWaitMax = 6.0f;
            const float defReactionBase = 1.25f;

            const float defCatchDistanceBase = 0.075f;
            const float defSpawnDistanceBase = 0.62f;
            const float defSpawnJitter = 0.16f;

            const int defTensionMaxTicks = 4;

            const float defActionMinSeconds = 1.30f;
            const float defActionMaxSeconds = 2.60f;

            const float defMoveSpeedMin = 0.16f;
            const float defMoveSpeedMax = 0.34f;

            const float defBurstSpeedMin = 0.42f;
            const float defBurstSpeedMax = 0.80f;

            const float defFailGraceSeconds = 0.28f;

            const float defLoosenPenaltyMin = 0.015f;
            const float defLoosenPenaltyMax = 0.045f;

            const float defFatigueGainOnCorrect = 0.10f;
            const float defFatigueLossOnWrong = 0.18f;
            const float defFatigueLossOnLoosen = 0.35f;

            float GetBiteMin() => GetCustomFloat(KeyFishingBiteWaitMin, defBiteWaitMin, min: 1.0f, max: 5.0f);
            float GetBiteMax() => GetCustomFloat(KeyFishingBiteWaitMax, defBiteWaitMax, min: 2.0f, max: 8.0f);

            float GetActionMin() => GetCustomFloat(KeyFishingActionMinSeconds, defActionMinSeconds, min: 0.75f, max: 2.00f);
            float GetActionMax() => GetCustomFloat(KeyFishingActionMaxSeconds, defActionMaxSeconds, min: 1.50f, max: 3.25f);

            float GetMoveMin() => GetCustomFloat(KeyFishingMoveLateralSpeedMin, defMoveSpeedMin, min: 0.10f, max: 0.40f);
            float GetMoveMax() => GetCustomFloat(KeyFishingMoveLateralSpeedMax, defMoveSpeedMax, min: 0.20f, max: 0.60f);

            float GetBurstMin() => GetCustomFloat(KeyFishingBurstForwardSpeedMin, defBurstSpeedMin, min: 0.20f, max: 0.90f);
            float GetBurstMax() => GetCustomFloat(KeyFishingBurstForwardSpeedMax, defBurstSpeedMax, min: 0.40f, max: 1.20f);

            float GetLoosenMin() => GetCustomFloat(KeyFishingLoosenDistancePenaltyMin, defLoosenPenaltyMin, min: 0.00f, max: 0.08f);
            float GetLoosenMax() => GetCustomFloat(KeyFishingLoosenDistancePenaltyMax, defLoosenPenaltyMax, min: 0.00f, max: 0.08f);

            return new SettingsFolder(
                labelKey: "settings.base",
                descriptionKey: "settings.base.desc",
                buildChildren: () =>
                {
                    var items = new List<SettingsItem>
                    {
                        BuildFloatRangeSetting(
                            labelKey: "settings.base.bite_wait_min",
                            descriptionKey: "settings.base.bite_wait_min.desc",
                            customKey: KeyFishingBiteWaitMin,
                            min: 1.0f, max: 5.0f, step: 0.5f,
                            defaultValue: defBiteWaitMin),

                        BuildFloatRangeSettingDynamic(
                            labelKey: "settings.base.bite_wait_max",
                            descriptionKey: "settings.base.bite_wait_max.desc",
                            customKey: KeyFishingBiteWaitMax,
                            minProvider: () => Mathf.Clamp(GetBiteMin(), 1.0f, 5.0f),
                            maxProvider: () => 8.0f,
                            step: 0.5f,
                            defaultValue: defBiteWaitMax),

                        BuildFloatRangeSetting(
                            labelKey: "settings.base.reaction_window_base",
                            descriptionKey: "settings.base.reaction_window_base.desc",
                            customKey: KeyFishingReactionWindowBase,
                            min: 0.75f, max: 1.75f, step: 0.05f,
                            defaultValue: defReactionBase),

                        BuildFloatRangeSetting(
                            labelKey: "settings.base.catch_distance_base",
                            descriptionKey: "settings.base.catch_distance_base.desc",
                            customKey: KeyFishingCatchDistanceBase,
                            min: 0.05f, max: 0.10f, step: 0.005f,
                            defaultValue: defCatchDistanceBase),

                        BuildFloatRangeSetting(
                            labelKey: "settings.base.spawn_distance_base",
                            descriptionKey: "settings.base.spawn_distance_base.desc",
                            customKey: KeyFishingSpawnDistanceBase,
                            min: 0.56f, max: 0.68f, step: 0.01f,
                            defaultValue: defSpawnDistanceBase),

                        BuildFloatRangeSetting(
                            labelKey: "settings.base.spawn_distance_jitter",
                            descriptionKey: "settings.base.spawn_distance_jitter.desc",
                            customKey: KeyFishingSpawnDistanceJitter,
                            min: 0.10f, max: 0.22f, step: 0.01f,
                            defaultValue: defSpawnJitter),

                        new SettingsFolder(
                            labelKey: "settings.advanced",
                            descriptionKey: "settings.advanced.desc",
                            buildChildren: () => new List<SettingsItem>
                            {
                                BuildIntRangeSetting(
                                    labelKey: "settings.base.tension_max_ticks",
                                    descriptionKey: "settings.base.tension_max_ticks.desc",
                                    customKey: KeyFishingTensionMaxTicks,
                                    min: 3, max: 5, step: 1,
                                    defaultValue: defTensionMaxTicks),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.fail_grace",
                                    descriptionKey: "settings.base.fail_grace.desc",
                                    customKey: KeyFishingFailGraceSeconds,
                                    min: 0.12f, max: 0.40f, step: 0.02f,
                                    defaultValue: defFailGraceSeconds),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.action_min_seconds",
                                    descriptionKey: "settings.base.action_min_seconds.desc",
                                    customKey: KeyFishingActionMinSeconds,
                                    min: 0.75f, max: 2.00f, step: 0.05f,
                                    defaultValue: defActionMinSeconds),

                                BuildFloatRangeSettingDynamic(
                                    labelKey: "settings.base.action_max_seconds",
                                    descriptionKey: "settings.base.action_max_seconds.desc",
                                    customKey: KeyFishingActionMaxSeconds,
                                    minProvider: () => Mathf.Clamp(GetActionMin(), 0.75f, 2.00f),
                                    maxProvider: () => 3.25f,
                                    step: 0.05f,
                                    defaultValue: defActionMaxSeconds),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.move_speed_min",
                                    descriptionKey: "settings.base.move_speed_min.desc",
                                    customKey: KeyFishingMoveLateralSpeedMin,
                                    min: 0.10f, max: 0.40f, step: 0.02f,
                                    defaultValue: defMoveSpeedMin),

                                BuildFloatRangeSettingDynamic(
                                    labelKey: "settings.base.move_speed_max",
                                    descriptionKey: "settings.base.move_speed_max.desc",
                                    customKey: KeyFishingMoveLateralSpeedMax,
                                    minProvider: () => Mathf.Clamp(GetMoveMin(), 0.10f, 0.40f),
                                    maxProvider: () => 0.60f,
                                    step: 0.02f,
                                    defaultValue: defMoveSpeedMax),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.burst_speed_min",
                                    descriptionKey: "settings.base.burst_speed_min.desc",
                                    customKey: KeyFishingBurstForwardSpeedMin,
                                    min: 0.20f, max: 0.90f, step: 0.02f,
                                    defaultValue: defBurstSpeedMin),

                                BuildFloatRangeSettingDynamic(
                                    labelKey: "settings.base.burst_speed_max",
                                    descriptionKey: "settings.base.burst_speed_max.desc",
                                    customKey: KeyFishingBurstForwardSpeedMax,
                                    minProvider: () => Mathf.Clamp(GetBurstMin(), 0.20f, 0.90f),
                                    maxProvider: () => 1.20f,
                                    step: 0.02f,
                                    defaultValue: defBurstSpeedMax),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.loosen_penalty_min",
                                    descriptionKey: "settings.base.loosen_penalty_min.desc",
                                    customKey: KeyFishingLoosenDistancePenaltyMin,
                                    min: 0.00f, max: 0.08f, step: 0.005f,
                                    defaultValue: defLoosenPenaltyMin),

                                BuildFloatRangeSettingDynamic(
                                    labelKey: "settings.base.loosen_penalty_max",
                                    descriptionKey: "settings.base.loosen_penalty_max.desc",
                                    customKey: KeyFishingLoosenDistancePenaltyMax,
                                    minProvider: () => Mathf.Clamp(GetLoosenMin(), 0.00f, 0.08f),
                                    maxProvider: () => 0.08f,
                                    step: 0.005f,
                                    defaultValue: defLoosenPenaltyMax),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.fatigue_gain_correct",
                                    descriptionKey: "settings.base.fatigue_gain_correct.desc",
                                    customKey: KeyFishingFatigueGainOnCorrect,
                                    min: 0.00f, max: 0.30f, step: 0.01f,
                                    defaultValue: defFatigueGainOnCorrect),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.fatigue_loss_wrong",
                                    descriptionKey: "settings.base.fatigue_loss_wrong.desc",
                                    customKey: KeyFishingFatigueLossOnWrong,
                                    min: 0.00f, max: 0.40f, step: 0.01f,
                                    defaultValue: defFatigueLossOnWrong),

                                BuildFloatRangeSetting(
                                    labelKey: "settings.base.fatigue_loss_loosen",
                                    descriptionKey: "settings.base.fatigue_loss_loosen.desc",
                                    customKey: KeyFishingFatigueLossOnLoosen,
                                    min: 0.00f, max: 0.60f, step: 0.01f,
                                    defaultValue: defFatigueLossOnLoosen),

                                new SettingsAction("common.back", execute: () => { })
                            }
                        ),

                        new SettingsAction("common.back", execute: () => { })
                    };

                    return items;
                }
            );
        }

        private SettingsFolder BuildModesFolderIfAny(string gameId, GameDefinition game)
        {
            if (game == null || game.modes == null || game.modes.Length == 0)
                return null;

            var modeFolders = new List<SettingsItem>();

            for (int i = 0; i < game.modes.Length; i++)
            {
                var m = game.modes[i];
                if (m == null || string.IsNullOrWhiteSpace(m.modeId))
                    continue;

                if (string.Equals(m.modeId, "custom", StringComparison.OrdinalIgnoreCase))
                    continue;

                var folder = BuildModeFolderIfAny(gameId, m.modeId);
                if (folder != null)
                    modeFolders.Add(folder);
            }

            if (modeFolders.Count == 0)
                return null;

            modeFolders.Add(new SettingsAction("common.back", execute: () => { }));

            return new SettingsFolder(
                labelKey: "settings.modes",
                descriptionKey: "settings.modes.desc",
                buildChildren: () => modeFolders
            );
        }

        private SettingsFolder BuildModeFolderIfAny(string gameId, string modeId)
        {
            if (string.Equals(gameId, MemoryGameId, StringComparison.OrdinalIgnoreCase))
                return BuildMemoryModeFolder(modeId);

            if (string.Equals(gameId, SteamRushGameId, StringComparison.OrdinalIgnoreCase))
                return BuildSteamRushModeFolder(modeId);

            if (string.Equals(gameId, FishingGameId, StringComparison.OrdinalIgnoreCase))
                return BuildFishingModeFolder(modeId);

            return null;
        }

        private SettingsFolder BuildSteamRushModeFolder(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return null;

            bool endless = IsEndlessMode(modeId);

            float defDifficulty = ResolveSteamRushDefaultDifficultyScale(modeId);
            int defPatternTierMax = ResolveSteamRushDefaultPatternTierMax(modeId);
            float defDuration = ResolveSteamRushDefaultDurationSeconds(modeId);
            const float defWhistleMoment = 0.50f;

            string keyDifficulty = BuildSteamRushPerModeKey(modeId, KeyDifficultyScaleSuffix);
            string keyPatternTierMax = BuildSteamRushPerModeKey(modeId, KeyPatternTierMaxSuffix);
            string keyDuration = BuildSteamRushPerModeKey(modeId, KeyModeDurationSecondsSuffix);
            string keyWhistle = BuildSteamRushPerModeKey(modeId, KeyWhistleMomentSuffix);

            return new SettingsFolder(
                labelKey: $"mode.{modeId}",
                descriptionKey: null,
                buildChildren: () =>
                {
                    var items = new List<SettingsItem>
                    {
                        BuildFloatRangeSetting(
                            labelKey: "settings.mode.difficulty_scale",
                            descriptionKey: "settings.mode.difficulty_scale.desc",
                            customKey: keyDifficulty,
                            min: 0.30f, max: 2.50f, step: 0.05f,
                            defaultValue: defDifficulty),

                        BuildIntRangeSetting(
                            labelKey: "settings.mode.pattern_tier_max",
                            descriptionKey: "settings.mode.pattern_tier_max.desc",
                            customKey: keyPatternTierMax,
                            min: 0, max: 3, step: 1,
                            defaultValue: defPatternTierMax)
                    };

                    if (!endless)
                    {
                        items.Add(
                            BuildFloatRangeSetting(
                                labelKey: "settings.mode.duration",
                                descriptionKey: "settings.mode.duration.desc",
                                customKey: keyDuration,
                                min: 10.0f, max: 600.0f, step: 5.0f,
                                defaultValue: defDuration
                            )
                        );
                    }

                    items.Add(
                        BuildFloatRangeSetting(
                            labelKey: "settings.mode.whistle_moment",
                            descriptionKey: "settings.mode.whistle_moment.desc",
                            customKey: keyWhistle,
                            min: 0.10f, max: 0.90f, step: 0.05f,
                            defaultValue: defWhistleMoment
                        )
                    );

                    items.Add(new SettingsAction("common.back", execute: () => { }));
                    return items;
                }
            );
        }

        private static bool IsEndlessMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId)) return false;
            var id = modeId.Trim();

            return string.Equals(id, "endless", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "nieskonczony", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "nieskoñczony", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSteamRushPerModeKey(string modeId, string suffix)
        {
            return SteamRushPerModePrefix + modeId + SteamRushPerModeMid + suffix;
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

        private SettingsFolder BuildFishingModeFolder(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return null;

            ResolveFishingDefaults(modeId,
                out float defDifficultyScale,
                out int defTargetFishCount,
                out float defAggMin,
                out float defAggMax,
                out float defResMin,
                out float defResMax,
                out float defReactionScale);

            string keyDifficulty = BuildFishingPerModeKey(modeId, KeyFishingModeDifficultyScaleSuffix);
            string keyTarget = BuildFishingPerModeKey(modeId, KeyFishingModeTargetFishCountSuffix);
            string keyAggMin = BuildFishingPerModeKey(modeId, KeyFishingModeAggressionMinSuffix);
            string keyAggMax = BuildFishingPerModeKey(modeId, KeyFishingModeAggressionMaxSuffix);
            string keyResMin = BuildFishingPerModeKey(modeId, KeyFishingModeResistanceMinSuffix);
            string keyResMax = BuildFishingPerModeKey(modeId, KeyFishingModeResistanceMaxSuffix);
            string keyReactionScale = BuildFishingPerModeKey(modeId, KeyFishingModeReactionWindowScaleSuffix);

            float GetAggMin() => GetCustomFloat(keyAggMin, defAggMin, min: 0.0f, max: 1.0f);
            float GetResMin() => GetCustomFloat(keyResMin, defResMin, min: 0.70f, max: 2.00f);

            return new SettingsFolder(
                labelKey: $"mode.{modeId}",
                descriptionKey: null,
                buildChildren: () => new List<SettingsItem>
                {
                    BuildFloatRangeSetting(
                        labelKey: "settings.mode.difficulty_scale",
                        descriptionKey: "settings.mode.difficulty_scale.desc",
                        customKey: keyDifficulty,
                        min: 0.50f, max: 1.35f, step: 0.05f,
                        defaultValue: defDifficultyScale),

                    BuildIntRangeSetting(
                        labelKey: "settings.mode.target_fish_count",
                        descriptionKey: "settings.mode.target_fish_count.desc",
                        customKey: keyTarget,
                        min: 1, max: 50, step: 1,
                        defaultValue: defTargetFishCount),

                    BuildFloatRangeSetting(
                        labelKey: "settings.mode.reaction_window_scale",
                        descriptionKey: "settings.mode.reaction_window_scale.desc",
                        customKey: keyReactionScale,
                        min: 0.80f, max: 1.40f, step: 0.02f,
                        defaultValue: defReactionScale),

                    BuildFloatRangeSetting(
                        labelKey: "settings.mode.aggression_min",
                        descriptionKey: "settings.mode.aggression_min.desc",
                        customKey: keyAggMin,
                        min: 0.00f, max: 1.00f, step: 0.05f,
                        defaultValue: defAggMin),

                    BuildFloatRangeSettingDynamic(
                        labelKey: "settings.mode.aggression_max",
                        descriptionKey: "settings.mode.aggression_max.desc",
                        customKey: keyAggMax,
                        minProvider: () => Mathf.Clamp(GetAggMin(), 0.00f, 1.00f),
                        maxProvider: () => 1.00f,
                        step: 0.05f,
                        defaultValue: defAggMax),

                    BuildFloatRangeSetting(
                        labelKey: "settings.mode.resistance_min",
                        descriptionKey: "settings.mode.resistance_min.desc",
                        customKey: keyResMin,
                        min: 0.70f, max: 2.00f, step: 0.05f,
                        defaultValue: defResMin),

                    BuildFloatRangeSettingDynamic(
                        labelKey: "settings.mode.resistance_max",
                        descriptionKey: "settings.mode.resistance_max.desc",
                        customKey: keyResMax,
                        minProvider: () => Mathf.Clamp(GetResMin(), 0.70f, 2.00f),
                        maxProvider: () => 2.00f,
                        step: 0.05f,
                        defaultValue: defResMax),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static string BuildFishingPerModeKey(string modeId, string suffix)
        {
            return SteamRushPerModePrefix + modeId + FishingPerModeMid + suffix;
        }

        private static void ResolveFishingDefaults(
            string modeId,
            out float difficultyScale,
            out int targetFishCount,
            out float aggressionMin,
            out float aggressionMax,
            out float resistanceMin,
            out float resistanceMax,
            out float reactionWindowScale)
        {
            string id = string.IsNullOrWhiteSpace(modeId) ? "easy" : modeId.Trim();

            if (string.Equals(id, "tutorial", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "samouczek", StringComparison.OrdinalIgnoreCase))
            {
                difficultyScale = 0.50f;
                targetFishCount = 1;
                aggressionMin = 0.00f;
                aggressionMax = 0.20f;
                resistanceMin = 0.70f;
                resistanceMax = 0.95f;
                reactionWindowScale = 1.30f;
                return;
            }

            if (string.Equals(id, "easy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "latwy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "³atwy", StringComparison.OrdinalIgnoreCase))
            {
                difficultyScale = 0.80f;
                targetFishCount = 3;
                aggressionMin = 0.10f;
                aggressionMax = 0.55f;
                resistanceMin = 0.90f;
                resistanceMax = 1.25f;
                reactionWindowScale = 1.10f;
                return;
            }

            if (string.Equals(id, "medium", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "sredni", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "œredni", StringComparison.OrdinalIgnoreCase))
            {
                difficultyScale = 1.10f;
                targetFishCount = 5;
                aggressionMin = 0.25f;
                aggressionMax = 0.80f;
                resistanceMin = 1.05f;
                resistanceMax = 1.55f;
                reactionWindowScale = 1.00f;
                return;
            }

            if (string.Equals(id, "hard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "trudny", StringComparison.OrdinalIgnoreCase))
            {
                difficultyScale = 1.35f;
                targetFishCount = 7;
                aggressionMin = 0.55f;
                aggressionMax = 0.98f;
                resistanceMin = 1.20f;
                resistanceMax = 1.90f;
                reactionWindowScale = 0.92f;
                return;
            }

            difficultyScale = 1.10f;
            targetFishCount = 5;
            aggressionMin = 0.25f;
            aggressionMax = 0.80f;
            resistanceMin = 1.05f;
            resistanceMax = 1.55f;
            reactionWindowScale = 1.00f;
        }

        private SettingsFolder BuildMemoryModeFolder(string modeId)
        {
            ResolveMemoryDefaultBoardSize(modeId, out string defW, out string defH);

            return new SettingsFolder(
                labelKey: $"mode.{modeId}",
                descriptionKey: null,
                buildChildren: () => new List<SettingsItem>
                {
                    BuildStringRangeSetting(
                        labelKey: "settings.board.width",
                        descriptionKey: "settings.board.width.desc",
                        customKey: $"mode.{modeId}.board.width",
                        min: 2, max: 8, step: 1, defaultValue: defW),

                    BuildStringRangeSetting(
                        labelKey: "settings.board.height",
                        descriptionKey: "settings.board.height.desc",
                        customKey: $"mode.{modeId}.board.height",
                        min: 2, max: 8, step: 1, defaultValue: defH),

                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private static void ResolveMemoryDefaultBoardSize(string modeId, out string width, out string height)
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
        }

        private SettingsFolder BuildCustomFolderIfAny(string gameId, GameDefinition game)
        {
            if (!GameHasMode(game, "custom"))
                return null;

            var items = BuildCustomItemsIfAny(gameId);
            if (items == null || items.Count == 0)
                return null;

            items.Add(new SettingsAction("common.back", execute: () => { }));

            return new SettingsFolder(
                labelKey: "settings.custom",
                descriptionKey: "settings.custom.desc",
                buildChildren: () => items
            );
        }

        private List<SettingsItem> BuildCustomItemsIfAny(string gameId)
        {
            if (string.Equals(gameId, MemoryGameId, StringComparison.OrdinalIgnoreCase))
            {
                const string defaultCustom = "6";

                return new List<SettingsItem>
                {
                    BuildStringRangeSetting(
                        labelKey: "settings.board.width",
                        descriptionKey: "settings.board.width.desc",
                        customKey: "custom.board.width",
                        min: 2, max: 8, step: 1, defaultValue: defaultCustom),

                    BuildStringRangeSetting(
                        labelKey: "settings.board.height",
                        descriptionKey: "settings.board.height.desc",
                        customKey: "custom.board.height",
                        min: 2, max: 8, step: 1, defaultValue: defaultCustom)
                };
            }

            if (string.Equals(gameId, FishingGameId, StringComparison.OrdinalIgnoreCase))
            {
                const int defTargetFishCount = 5;
                const string defPresetId = "medium";
                const float defAggressionMax = 0.65f;

                return new List<SettingsItem>
                {
                    BuildIntRangeSetting(
                        labelKey: "settings.custom.target_fish_count",
                        descriptionKey: "settings.custom.target_fish_count.desc",
                        customKey: KeyFishingTargetFishCount,
                        min: 1, max: 10, step: 1,
                        defaultValue: defTargetFishCount),

                    BuildFishingDifficultyPresetList(
                        labelKey: "settings.custom.difficulty_preset",
                        descriptionKey: "settings.custom.difficulty_preset.desc",
                        customKey: KeyFishingDifficultyPreset,
                        defaultPresetId: defPresetId),

                    BuildFloatRangeSetting(
                        labelKey: "settings.custom.aggression_max",
                        descriptionKey: "settings.custom.aggression_max.desc",
                        customKey: KeyFishingAggressionMax,
                        min: 0.00f, max: 1.00f, step: 0.05f,
                        defaultValue: defAggressionMax)
                };
            }

            return null;
        }

        private SettingsList BuildFishingDifficultyPresetList(
            string labelKey,
            string descriptionKey,
            string customKey,
            string defaultPresetId)
        {
            return new SettingsList(
                labelKey: labelKey,
                descriptionKey: descriptionKey,
                getOptions: () =>
                {
                    return new List<SettingsListOption>
                    {
                        new SettingsListOption(id: "tutorial", labelKey: "settings.preset.tutorial"),
                        new SettingsListOption(id: "easy", labelKey: "settings.preset.easy"),
                        new SettingsListOption(id: "medium", labelKey: "settings.preset.medium"),
                        new SettingsListOption(id: "hard", labelKey: "settings.preset.hard"),
                    };
                },
                getIndex: () =>
                {
                    string raw = GetCustomString(customKey, defaultPresetId);
                    string id = NormalizePresetId(raw, fallback: defaultPresetId);

                    var opts = new[] { "tutorial", "easy", "medium", "hard" };
                    for (int i = 0; i < opts.Length; i++)
                        if (string.Equals(opts[i], id, StringComparison.OrdinalIgnoreCase))
                            return i;

                    return 2;
                },
                setIndex: idx =>
                {
                    var opts = new[] { "tutorial", "easy", "medium", "hard" };

                    if (idx < 0) idx = 0;
                    if (idx >= opts.Length) idx = opts.Length - 1;

                    SetCustomString(customKey, opts[idx]);
                }
            );
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

        private static bool GameHasMode(GameDefinition game, string modeId)
        {
            if (game == null || game.modes == null || string.IsNullOrWhiteSpace(modeId))
                return false;

            for (int i = 0; i < game.modes.Length; i++)
            {
                var m = game.modes[i];
                if (m == null) continue;
                if (string.Equals(m.modeId, modeId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private SettingsFolder BuildStatsFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.stats",
                descriptionKey: "settings.stats.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildRecentCapacityRange(),
                    BuildResetStatsAction(),
                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsRange BuildRecentCapacityRange()
        {
            return new SettingsRange(
                labelKey: "settings.stats.recent_capacity",
                descriptionKey: "settings.stats.recent_capacity.desc",
                min: 1f,
                max: 10f,
                step: 1f,
                getValue: () =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;
                    int v = _prefs.GetRecentCapacity(gameId);
                    if (v < 1) v = 1;
                    if (v > 10) v = 10;
                    return v;
                },
                setValue: v =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;

                    int cap = (int)v;
                    if (cap < 1) cap = 1;
                    if (cap > 10) cap = 10;

                    _prefs.SetRecentCapacity(gameId, cap);
                }
            );
        }

        private SettingsAction BuildResetStatsAction()
        {
            return new SettingsAction(
                labelKey: "settings.stats.reset",
                descriptionKey: "settings.stats.reset.desc",
                execute: () =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;
                    _stats.Reset(gameId);
                }
            );
        }

        private SettingsAction BuildResetSettingsAction(string gameId, bool includeDeveloper)
        {
            return new SettingsAction(
                labelKey: "settings.reset_defaults",
                descriptionKey: includeDeveloper
                    ? "settings.reset_defaults.desc.dev"
                    : "settings.reset_defaults.desc",
                execute: () =>
                {
                    ResetGameSettings(gameId, includeDeveloper);
                }
            );
        }

        private void ResetGameSettings(string gameId, bool includeDeveloper)
        {
            var g = GetOrCreateGameEntry(gameId);

            if (g != null)
            {
                RemoveCustomByPrefix(g, "custom.");
            }

            if (includeDeveloper && g != null)
            {
                if (g.prefs == null)
                    g.prefs = new GamePreferencesData();

                g.prefs.useRandomSeed = true;
                g.prefs.hasSelectedSeed = false;
                g.prefs.selectedSeed = 0;

                RemoveCustomByPrefix(g, "mode.");
                RemoveCustomByPrefix(g, "steamrush.");
                RemoveCustomByPrefix(g, "fishing.");
            }

            SaveStoreSafe();
        }

        private SettingsRange BuildStringRangeSetting(
            string labelKey,
            string descriptionKey,
            string customKey,
            int min,
            int max,
            int step,
            string defaultValue)
        {
            return new SettingsRange(
                labelKey: labelKey,
                descriptionKey: descriptionKey,
                min: min,
                max: max,
                step: step,
                getValue: () =>
                {
                    string s = GetCustomString(customKey, defaultValue);
                    if (!int.TryParse(s, out int v))
                        v = SafeParseDefaultInt(defaultValue, fallback: min);

                    if (v < min) v = min;
                    if (v > max) v = max;
                    return v;
                },
                setValue: v =>
                {
                    int iv = (int)v;
                    if (iv < min) iv = min;
                    if (iv > max) iv = max;
                    SetCustomString(customKey, iv.ToString());
                }
            );
        }

        private SettingsRange BuildIntRangeSetting(
            string labelKey,
            string descriptionKey,
            string customKey,
            int min,
            int max,
            int step,
            int defaultValue)
        {
            if (max < min) (min, max) = (max, min);
            if (step <= 0) step = 1;

            return new SettingsRange(
                labelKey: labelKey,
                descriptionKey: descriptionKey,
                min: min,
                max: max,
                step: step,
                getValue: () =>
                {
                    string s = GetCustomString(customKey, defaultValue.ToString(CultureInfo.InvariantCulture));
                    if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                        v = defaultValue;

                    if (v < min) v = min;
                    if (v > max) v = max;
                    return v;
                },
                setValue: v =>
                {
                    int iv = (int)v;
                    if (iv < min) iv = min;
                    if (iv > max) iv = max;

                    SetCustomString(customKey, iv.ToString(CultureInfo.InvariantCulture));
                }
            );
        }

        private SettingsRange BuildFloatRangeSetting(
            string labelKey,
            string descriptionKey,
            string customKey,
            float min,
            float max,
            float step,
            float defaultValue)
        {
            if (max < min) (min, max) = (max, min);
            if (step <= 0f) step = 0.01f;

            return new SettingsRange(
                labelKey: labelKey,
                descriptionKey: descriptionKey,
                min: min,
                max: max,
                step: step,
                getValue: () =>
                {
                    string s = GetCustomString(customKey, defaultValue.ToString("0.###", CultureInfo.InvariantCulture));

                    float v = defaultValue;
                    try
                    {
                        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                            v = defaultValue;
                    }
                    catch { v = defaultValue; }

                    if (float.IsNaN(v) || float.IsInfinity(v))
                        v = defaultValue;

                    v = Mathf.Clamp(v, min, max);
                    return v;
                },
                setValue: v =>
                {
                    float fv = v;
                    if (float.IsNaN(fv) || float.IsInfinity(fv))
                        fv = defaultValue;

                    fv = Mathf.Clamp(fv, min, max);

                    SetCustomString(customKey, fv.ToString("0.###", CultureInfo.InvariantCulture));
                }
            );
        }

        private SettingsRange BuildFloatRangeSettingDynamic(
            string labelKey,
            string descriptionKey,
            string customKey,
            Func<float> minProvider,
            Func<float> maxProvider,
            float step,
            float defaultValue)
        {
            if (minProvider == null) minProvider = () => 0f;
            if (maxProvider == null) maxProvider = () => 1f;
            if (step <= 0f) step = 0.01f;

            return new SettingsRange(
                labelKey: labelKey,
                descriptionKey: descriptionKey,
                minProvider: minProvider,
                maxProvider: maxProvider,
                step: step,
                getValue: () =>
                {
                    float min = minProvider();
                    float max = maxProvider();
                    if (max < min) (min, max) = (max, min);

                    string s = GetCustomString(customKey, defaultValue.ToString("0.###", CultureInfo.InvariantCulture));

                    float v = defaultValue;
                    try
                    {
                        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                            v = defaultValue;
                    }
                    catch { v = defaultValue; }

                    if (float.IsNaN(v) || float.IsInfinity(v))
                        v = defaultValue;

                    v = Mathf.Clamp(v, min, max);
                    return v;
                },
                setValue: v =>
                {
                    float min = minProvider();
                    float max = maxProvider();
                    if (max < min) (min, max) = (max, min);

                    float fv = v;
                    if (float.IsNaN(fv) || float.IsInfinity(fv))
                        fv = defaultValue;

                    fv = Mathf.Clamp(fv, min, max);

                    SetCustomString(customKey, fv.ToString("0.###", CultureInfo.InvariantCulture));
                }
            );
        }

        private static int SafeParseDefaultInt(string s, int fallback)
        {
            if (int.TryParse(s, out int v)) return v;
            return fallback;
        }

        private float GetCustomFloat(string key, float defaultValue, float min, float max)
        {
            string s = GetCustomString(key, defaultValue.ToString("0.###", CultureInfo.InvariantCulture));
            float v = defaultValue;

            try
            {
                if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    v = defaultValue;
            }
            catch { v = defaultValue; }

            if (float.IsNaN(v) || float.IsInfinity(v))
                v = defaultValue;

            v = Mathf.Clamp(v, min, max);
            return v;
        }

        private string GetCustomString(string key, string defaultValue)
        {
            string gameId = _session != null ? _session.SelectedGameId : null;
            var g = GetOrCreateGameEntry(gameId);
            if (g == null) return defaultValue;

            var entry = FindCustomEntry(g, key);
            if (entry == null || string.IsNullOrWhiteSpace(entry.jsonValue))
                return defaultValue;

            return entry.jsonValue;
        }

        private void SetCustomString(string key, string value)
        {
            string gameId = _session != null ? _session.SelectedGameId : null;
            var g = GetOrCreateGameEntry(gameId);
            if (g == null) return;

            EnsureCustomList(g);

            var entry = FindCustomEntry(g, key);
            if (entry == null)
            {
                entry = new GameCustomEntry { key = key, jsonValue = value ?? "" };
                g.custom.Add(entry);
            }
            else
            {
                entry.jsonValue = value ?? "";
            }

            SaveStoreSafe();
        }

        private static GameCustomEntry FindCustomEntry(GameUserEntry g, string key)
        {
            if (g == null || g.custom == null || string.IsNullOrWhiteSpace(key))
                return null;

            for (int i = 0; i < g.custom.Count; i++)
            {
                var e = g.custom[i];
                if (e != null && e.key == key)
                    return e;
            }

            return null;
        }

        private static void EnsureCustomList(GameUserEntry g)
        {
            if (g.custom == null)
                g.custom = new List<GameCustomEntry>();
        }

        private static void RemoveCustomByPrefix(GameUserEntry g, string prefix)
        {
            if (g == null || g.custom == null || string.IsNullOrWhiteSpace(prefix))
                return;

            for (int i = g.custom.Count - 1; i >= 0; i--)
            {
                var e = g.custom[i];
                if (e != null && !string.IsNullOrWhiteSpace(e.key) && e.key.StartsWith(prefix, StringComparison.Ordinal))
                    g.custom.RemoveAt(i);
            }
        }

        private GameUserEntry GetOrCreateGameEntry(string gameId)
        {
            if (_store == null || string.IsNullOrWhiteSpace(gameId))
                return null;

            try { return _store.GetOrCreateGame(gameId); }
            catch { return null; }
        }

        private void SaveStoreSafe()
        {
            try { _store?.Save(); } catch { }
        }
    }
}