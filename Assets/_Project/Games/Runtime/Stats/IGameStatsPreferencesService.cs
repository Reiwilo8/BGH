namespace Project.Games.Stats
{
    public interface IGameStatsPreferencesService
    {
        int GetRecentCapacity(string gameId);
        void SetRecentCapacity(string gameId, int capacity);
    }
}