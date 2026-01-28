using System.Collections.Generic;
using Project.Core.App;
using Project.Core.Settings.Ui;
using Project.Games.Stats;

namespace Project.Games.Module.Settings
{
    public sealed class GameSettingsRootBuilder : ISettingsRootBuilder
    {
        private readonly IGameStatsPreferencesService _prefs;
        private readonly IGameStatsService _stats;
        private readonly AppSession _session;

        public GameSettingsRootBuilder()
        {
            var services = AppContext.Services;

            _session = services.Resolve<AppSession>();

            _prefs = services.Resolve<IGameStatsPreferencesService>();
            _stats = services.Resolve<IGameStatsService>();
        }

        public List<SettingsItem> BuildRoot(SettingsBuildContext context)
        {
            var root = new List<SettingsItem>
            {
                BuildStatsFolder(),
                new SettingsAction("common.back", execute: () => { })
            };

            return root;
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