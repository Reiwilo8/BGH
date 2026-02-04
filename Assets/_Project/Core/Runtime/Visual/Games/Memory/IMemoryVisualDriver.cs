namespace Project.Core.Visual.Games.Memory
{
    public interface IMemoryVisualDriver
    {
        void SetVisible(bool visible);

        void Swipe(MemoryVisualSwipe swipe);

        void SetCovered();
        void SetRevealed(string valueText);
        void SetMatched(string valueText);
    }
}