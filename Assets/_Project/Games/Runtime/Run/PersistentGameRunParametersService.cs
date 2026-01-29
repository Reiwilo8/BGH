using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Project.Games.Persistence;

namespace Project.Games.Run
{
    public sealed class PersistentGameRunParametersService : IGameRunParametersService
    {
        private const int MaxKnownSeedsPerGame = 50;

        private readonly IGameDataStore _store;

        public PersistentGameRunParametersService(IGameDataStore store)
        {
            _store = store;
        }

        public bool GetUseRandomSeed(string gameId)
        {
            var g = _store.GetOrCreateGame(gameId);
            if (g == null || g.prefs == null)
                return true;

            return g.prefs.useRandomSeed;
        }

        public void SetUseRandomSeed(string gameId, bool useRandom)
        {
            var g = _store.GetOrCreateGame(gameId);
            if (g == null) return;

            if (g.prefs == null)
                g.prefs = new GamePreferencesData();

            g.prefs.useRandomSeed = useRandom;
            _store.Save();
        }

        public IReadOnlyList<int> GetKnownSeeds(string gameId)
        {
            var g = _store.GetOrCreateGame(gameId);
            if (g == null || g.prefs == null || g.prefs.knownSeeds == null || g.prefs.knownSeeds.Count == 0)
                return Array.Empty<int>();

            return g.prefs.knownSeeds;
        }

        public bool TryGetSelectedSeed(string gameId, out int seed)
        {
            seed = 0;

            var g = _store.GetOrCreateGame(gameId);
            if (g == null || g.prefs == null)
                return false;

            if (!g.prefs.hasSelectedSeed)
                return false;

            seed = g.prefs.selectedSeed;
            return true;
        }

        public void SetSelectedSeed(string gameId, int seed)
        {
            var g = _store.GetOrCreateGame(gameId);
            if (g == null) return;

            if (g.prefs == null)
                g.prefs = new GamePreferencesData();

            g.prefs.hasSelectedSeed = true;
            g.prefs.selectedSeed = seed;

            EnsureKnownSeedsList(g.prefs);
            InsertSeedMru(g.prefs.knownSeeds, seed);

            TrimKnownSeeds(g.prefs.knownSeeds);

            _store.Save();
        }

        public int ResolveSeedForNewRun(string gameId)
        {
            bool random = GetUseRandomSeed(gameId);
            if (random)
                return GenerateRandomSeed();

            if (TryGetSelectedSeed(gameId, out var selected))
                return selected;

            var known = GetKnownSeeds(gameId);
            if (known != null && known.Count > 0)
                return known[0];

            return GenerateRandomSeed();
        }

        public void AddKnownSeed(string gameId, int seed)
        {
            var g = _store.GetOrCreateGame(gameId);
            if (g == null) return;

            if (g.prefs == null)
                g.prefs = new GamePreferencesData();

            EnsureKnownSeedsList(g.prefs);

            bool changed = InsertSeedMru(g.prefs.knownSeeds, seed);
            TrimKnownSeeds(g.prefs.knownSeeds);

            if (changed)
                _store.Save();
        }

        private static void EnsureKnownSeedsList(GamePreferencesData prefs)
        {
            if (prefs.knownSeeds == null)
                prefs.knownSeeds = new List<int>();
        }

        private static bool InsertSeedMru(List<int> list, int seed)
        {
            if (list == null) return false;

            int idx = list.IndexOf(seed);
            if (idx == 0)
                return false;

            if (idx > 0)
                list.RemoveAt(idx);

            list.Insert(0, seed);
            return true;
        }

        private static void TrimKnownSeeds(List<int> list)
        {
            if (list == null) return;
            while (list.Count > MaxKnownSeedsPerGame)
                list.RemoveAt(list.Count - 1);
        }

        private static int GenerateRandomSeed()
        {
            try
            {
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }
            catch
            {
                return Guid.NewGuid().GetHashCode();
            }
        }
    }
}