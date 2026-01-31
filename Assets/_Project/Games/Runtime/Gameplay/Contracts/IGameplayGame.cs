using Project.Core.Input;
using System;

namespace Project.Games.Gameplay.Contracts
{
    public interface IGameplayGame
    {
        event Action<GameplayGameResult> GameFinished;

        void Initialize(GameplayGameContext context);

        void StartGame();
        void PauseGame();
        void ResumeGame();
        void StopGame();
    }
    public interface IGameplayInputHandler
    {
        void Handle(NavAction action);
    }
}