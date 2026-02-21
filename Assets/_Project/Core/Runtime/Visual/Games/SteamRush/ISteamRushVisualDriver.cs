using System;

namespace Project.Core.Visual.Games.SteamRush
{
    public interface ISteamRushVisualDriver
    {
        void SetVisible(bool visible);
        void Reset();
        void SetPaused(bool paused);

        void SetPlayerLane(int playerLane);

        void SpawnTrain(int lane, float approachSeconds, float passSeconds);
    }
}