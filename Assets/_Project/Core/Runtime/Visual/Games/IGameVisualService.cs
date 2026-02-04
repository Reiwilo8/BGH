using System;

namespace Project.Core.Visual.Games
{
    public interface IGameVisualService
    {
        bool HasActiveView { get; }

        void RegisterView(IGameVisualView view);
        void UnregisterView(IGameVisualView view);

        void Reset();
        void SetVisible(bool visible);

        void SubmitState(GameVisualState state);
        void Emit(GameVisualEvent e);
    }
}