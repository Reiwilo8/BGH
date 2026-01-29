using System;
using System.Collections.Generic;

namespace Project.Games.Persistence
{
    [Serializable]
    public sealed class GamesUserData
    {
        public int schemaVersion = 3;
        public List<GameUserEntry> games = new List<GameUserEntry>();
    }

    [Serializable]
    public sealed class GameUserEntry
    {
        public string gameId;

        public GameStatsData stats = new GameStatsData();
        public GamePreferencesData prefs = new GamePreferencesData();

        public List<GameCustomEntry> custom = new List<GameCustomEntry>();
    }

    [Serializable]
    public sealed class GamePreferencesData
    {
        public int recentCapacity = 5;

        public bool useRandomSeed = true;

        public bool hasSelectedSeed = false;
        public int selectedSeed = 0;

        public List<int> knownSeeds = new List<int>();
    }

    [Serializable]
    public sealed class GameStatsData
    {
        public List<GameModeStatsData> modes = new List<GameModeStatsData>();
    }

    [Serializable]
    public sealed class GameModeStatsData
    {
        public string modeId;

        public int runs;
        public int completions;

        public long bestCompletedTimeTicks;
        public long bestSurvivalTimeTicks;

        public long lastPlayedUtcTicks;

        public List<RecentRunData> recentRuns = new List<RecentRunData>();
    }

    [Serializable]
    public sealed class RecentRunData
    {
        public long durationTicks;
        public int score;
        public bool completed;
        public long finishedUtcTicks;
    }

    [Serializable]
    public sealed class GameCustomEntry
    {
        public string key;
        public string jsonValue;
    }
}