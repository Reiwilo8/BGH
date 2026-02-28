using Project.Core.Input;
using Project.Core.Input.Motion;
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

    public interface IGameplayDirection4Handler
    {
        void Handle(NavDirection4 direction);
    }

    public interface IGameplayMotionHandler
    {
        void Handle(MotionAction action);
    }
}