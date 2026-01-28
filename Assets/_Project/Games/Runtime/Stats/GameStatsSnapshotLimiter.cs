using System;
using System.Collections.Generic;

namespace Project.Games.Stats
{
    public static class GameStatsSnapshotLimiter
    {
        public static GameStatsSnapshot LimitRecent(GameStatsSnapshot snap, int capacity)
        {
            if (capacity < 1) capacity = 1;

            var perMode = snap.PerMode;
            if (perMode == null || perMode.Count == 0)
                return snap;

            var newPerMode = new List<ModeStatsSnapshot>(perMode.Count);
            bool changed = false;

            for (int i = 0; i < perMode.Count; i++)
            {
                var m = perMode[i];
                var rr = m.RecentRuns;

                if (rr == null || rr.Count <= capacity)
                {
                    newPerMode.Add(m);
                    continue;
                }

                changed = true;

                var limited = new RecentRunSnapshot[capacity];
                for (int j = 0; j < capacity; j++)
                    limited[j] = rr[j];

                newPerMode.Add(new ModeStatsSnapshot(
                    modeId: m.ModeId,
                    overall: m.Overall,
                    recentRuns: limited
                ));
            }

            if (!changed)
                return snap;

            return new GameStatsSnapshot(gameId: snap.GameId, perMode: newPerMode);
        }
    }
}