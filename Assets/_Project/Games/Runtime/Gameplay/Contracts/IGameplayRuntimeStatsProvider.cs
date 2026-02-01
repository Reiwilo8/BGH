using System.Collections.Generic;

namespace Project.Games.Gameplay.Contracts
{
    public interface IGameplayRuntimeStatsProvider
    {
        IReadOnlyDictionary<string, string> GetRuntimeStatsSnapshot();
    }

    public static class GameplayRuntimeStatsSnapshot
    {
        public static IReadOnlyDictionary<string, string> CopyOrNull(IReadOnlyDictionary<string, string> src)
        {
            if (src == null) return null;

            var dict = new Dictionary<string, string>(src.Count);
            foreach (var kv in src)
                dict[kv.Key] = kv.Value;

            return dict;
        }
    }
}