namespace Project.Core.Visual.Games.Fishing
{
    public interface IFishingVisualDriver
    {
        void SetVisible(bool visible);
        void Reset();
        void SetPaused(bool paused);
        void Apply(in FishingVisualState state);
        void PulseMistakeBlink();
        void PulseCatch();
    }
}