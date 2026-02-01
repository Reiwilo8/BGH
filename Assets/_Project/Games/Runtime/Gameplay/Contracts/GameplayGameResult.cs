using System.Collections.Generic;

namespace Project.Games.Gameplay.Contracts
{
    public enum GameplayGameFinishReason
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
        Quit = 3
    }

    public readonly struct GameplayGameResult
    {
        public readonly GameplayGameFinishReason Reason;
        public readonly int Score;

        public readonly IReadOnlyDictionary<string, string> RuntimeStats;

        public GameplayGameResult(
            GameplayGameFinishReason reason,
            int score = 0,
            IReadOnlyDictionary<string, string> runtimeStats = null)
        {
            Reason = reason;
            Score = score;
            RuntimeStats = runtimeStats;
        }
    }
}