using System.Threading.Tasks;

namespace Project.Core.App
{
    public interface IAppFlowService
    {
        bool IsTransitioning { get; }

        Task EnterStartAsync();
        Task EnterHubAsync();
        Task ExitApplicationAsync();
        Task EnterGameModuleAsync(string gameId);
        Task ExitGameModuleAsync();
        Task StartGameplayAsync(string gameplaySceneName);
        Task ReturnToGameModuleAsync();
        Task ReturnToHubAsync();
    }
}