using UnityEngine;
using Project.Core.App;
using Project.Games.Catalog;
using Project.Games.Persistence;
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

            var store = new PlayerPrefsGameDataStore();
            store.Load();

            AppContext.Services.Register<IGameDataStore>(store);
            AppContext.Services.Register<IGameStatsService>(new PersistentGameStatsService(store));
            AppContext.Services.Register<IGameStatsPreferencesService>(new PersistentGameStatsPreferencesService(store));
        }
    }
}