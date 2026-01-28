using Project.Games.Persistence;

namespace Project.Games.Stats
{
    public sealed class PersistentGameStatsPreferencesService : IGameStatsPreferencesService
    {
        private readonly IGameDataStore _store;

        public PersistentGameStatsPreferencesService(IGameDataStore store)
        {
            _store = store;
        }

        public int GetRecentCapacity(string gameId)
        {
            var g = _store.GetOrCreateGame(gameId);
            int v = g.prefs.recentCapacity;
            if (v < 1) return 1;
            if (v > 10) return 10;
            return v;
        }

        public void SetRecentCapacity(string gameId, int capacity)
        {
            var g = _store.GetOrCreateGame(gameId);

            if (capacity < 1) capacity = 1;
            if (capacity > 10) capacity = 10;

            g.prefs.recentCapacity = capacity;
            _store.Save();
        }
    }
}