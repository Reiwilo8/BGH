using System.Collections.Generic;

namespace Project.Games.Gameplay.Contracts
{
    public readonly struct GameplayGameContext
    {
        public readonly string GameId;
        public readonly string ModeId;
        public readonly string RunId;
        public readonly int? Seed;
        public readonly IReadOnlyDictionary<string, string> InitialParameters;

        public GameplayGameContext(
            string gameId,
            string modeId,
            string runId,
            int? seed,
            IReadOnlyDictionary<string, string> initialParameters)
        {
            GameId = gameId ?? "";
            ModeId = modeId ?? "";
            RunId = runId ?? "";
            Seed = seed;
            InitialParameters = initialParameters;
        }
    }
}