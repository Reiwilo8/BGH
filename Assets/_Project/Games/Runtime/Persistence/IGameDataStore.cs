namespace Project.Games.Persistence
{
    public interface IGameDataStore
    {
        GamesUserData Data { get; }

        void Load();
        void Save();

        GameUserEntry GetOrCreateGame(string gameId);
        void RemoveGame(string gameId);
    }
}