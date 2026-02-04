using System;
using System.Collections.Generic;
using Project.Core.App;
using Project.Core.Settings.Ui;
using Project.Games.Catalog;
using Project.Games.Definitions;
using Project.Games.Persistence;
using Project.Games.Run;
using Project.Games.Stats;

namespace Project.Games.Module.Settings
{
    public sealed class GameSettingsRootBuilder : ISettingsRootBuilder
    {
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
            if (string.Equals(gameId, "memory", StringComparison.OrdinalIgnoreCase))
                return BuildMemoryModeFolder(modeId);

            return null;
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
            if (string.Equals(gameId, "memory", StringComparison.OrdinalIgnoreCase))
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

            return null;
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

        private static int SafeParseDefaultInt(string s, int fallback)
        {
            if (int.TryParse(s, out int v)) return v;
            return fallback;
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