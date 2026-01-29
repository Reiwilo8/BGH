using System.Collections.Generic;

namespace Project.Games.Run
{
    public interface IGameRunParametersService
    {
        bool GetUseRandomSeed(string gameId);
        void SetUseRandomSeed(string gameId, bool useRandom);

        IReadOnlyList<int> GetKnownSeeds(string gameId);

        bool TryGetSelectedSeed(string gameId, out int seed);
        void SetSelectedSeed(string gameId, int seed);

        int ResolveSeedForNewRun(string gameId);

        void AddKnownSeed(string gameId, int seed);
    }
}