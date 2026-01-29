using System.Collections.Generic;
using Project.Core.App;
using Project.Core.Settings.Ui;
using Project.Games.Run;
using Project.Games.Stats;

namespace Project.Games.Module.Settings
{
    public sealed class GameSettingsRootBuilder : ISettingsRootBuilder
    {
        private readonly IGameStatsPreferencesService _prefs;
        private readonly IGameStatsService _stats;
        private readonly AppSession _session;

        private readonly IGameRunParametersService _runParams;

        public GameSettingsRootBuilder()
        {
            var services = AppContext.Services;

            _session = services.Resolve<AppSession>();

            _prefs = services.Resolve<IGameStatsPreferencesService>();
            _stats = services.Resolve<IGameStatsService>();

            try { _runParams = services.Resolve<IGameRunParametersService>(); }
            catch { _runParams = null; }
        }

        public List<SettingsItem> BuildRoot(SettingsBuildContext context)
        {
            var root = new List<SettingsItem>();

            if (IsDeveloperMode(context))
                root.Add(BuildRunParametersFolder());

            root.Add(BuildStatsFolder());
            root.Add(new SettingsAction("common.back", execute: () => { }));

            return root;
        }

        private static bool IsDeveloperMode(SettingsBuildContext context)
        {
            try { return context.DeveloperMode; }
            catch { return false; }
        }

        private SettingsFolder BuildRunParametersFolder()
        {
            return new SettingsFolder(
                labelKey: "settings.run_params",
                descriptionKey: "settings.run_params.desc",
                buildChildren: () => new List<SettingsItem>
                {
                    BuildUseRandomSeedToggle(),
                    BuildKnownSeedsList(),
                    new SettingsAction("common.back", execute: () => { })
                }
            );
        }

        private SettingsToggle BuildUseRandomSeedToggle()
        {
            return new SettingsToggle(
                labelKey: "settings.run_params.random_seed",
                descriptionKey: "settings.run_params.random_seed.desc",
                getValue: () =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;
                    if (_runParams == null) return true;
                    return _runParams.GetUseRandomSeed(gameId);
                },
                setValue: v =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;
                    _runParams?.SetUseRandomSeed(gameId, v);
                }
            );
        }

        private SettingsList BuildKnownSeedsList()
        {
            return new SettingsList(
                labelKey: "settings.run_params.seed_list",
                descriptionKey: "settings.run_params.seed_list.desc",
                getOptions: () =>
                {
                    string gameId = _session != null ? _session.SelectedGameId : null;

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
                    string gameId = _session != null ? _session.SelectedGameId : null;

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
                    string gameId = _session != null ? _session.SelectedGameId : null;

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
    }
}