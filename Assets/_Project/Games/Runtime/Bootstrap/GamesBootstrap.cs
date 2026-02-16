using UnityEngine;
using Project.Core.App;
using Project.Games.Catalog;
using Project.Games.Persistence;
using Project.Games.Run;
using Project.Games.Stats;

namespace Project.Games.Bootstrap
{
    public sealed class GamesBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog gameCatalog;

        private void Awake()
        {
            if (gameCatalog == null)
                throw new System.InvalidOperationException("GameCatalog is not assigned.");

            AppContext.Services.Register(gameCatalog);

            EnsureGameRunContextService();

            var store = new PlayerPrefsGameDataStore();
            store.Load();

            AppContext.Services.Register<IGameDataStore>(store);

            AppContext.Services.Register<IGameStatsService>(new PersistentGameStatsService(store));
            AppContext.Services.Register<IGameStatsPreferencesService>(new PersistentGameStatsPreferencesService(store));

            AppContext.Services.Register<IGameRunParametersService>(new PersistentGameRunParametersService(store));

            var initialParamsProvider = new CompositeGameInitialParametersProvider(
                new MemoryInitialParametersProvider(store),
                new SteamRushInitialParametersProvider(store)
            );
            AppContext.Services.Register<IGameInitialParametersProvider>(initialParamsProvider);

            WarnIfReporterMissing();
            WarnIfSeedReporterMissing();
        }

        private static void EnsureGameRunContextService()
        {
            try
            {
                AppContext.Services.Resolve<IGameRunContextService>();
            }
            catch
            {
                AppContext.Services.Register<IGameRunContextService>(new GameRunContextService());
            }
        }

        private void WarnIfReporterMissing()
        {
            if (GetComponent<GameRunStatsReporter>() == null)
            {
                Debug.LogWarning(
                    "[GamesBootstrap] GameRunStatsReporter is not present on AppRoot. " +
                    "Stats will not be recorded from run context events.");
            }
        }

        private void WarnIfSeedReporterMissing()
        {
            if (GetComponent<GameRunSeedHistoryReporter>() == null)
            {
                Debug.LogWarning(
                    "[GamesBootstrap] GameRunSeedHistoryReporter is not present on AppRoot. " +
                    "Known seed list will not be collected from run context events.");
            }
        }
    }
}