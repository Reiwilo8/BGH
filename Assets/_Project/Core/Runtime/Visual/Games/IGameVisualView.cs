namespace Project.Core.Visual.Games
{
    public interface IGameVisualView
    {
        void SetVisible(bool visible);
        void Reset();

        void ApplyState(GameVisualState state);
        void HandleEvent(GameVisualEvent e);
    }
}