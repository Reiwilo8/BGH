using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.App
{
    public sealed class AppFlowService : IAppFlowService
    {
        private readonly string _startScene;
        private readonly string _hubScene;
        private readonly string _gameModuleScene;

        private string _currentGameplayScene;

        public bool IsTransitioning { get; private set; }

        public AppFlowService(
            string startScene = "StartScene",
            string hubScene = "HubScene",
            string gameModuleScene = "GameModuleScene")
        {
            _startScene = startScene;
            _hubScene = hubScene;
            _gameModuleScene = gameModuleScene;
        }

        public async Task EnterStartAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_startScene);

            await EnsureUnloadedAsync(_hubScene);
            await EnsureUnloadedAsync(_gameModuleScene);

            if (!string.IsNullOrWhiteSpace(_currentGameplayScene))
                await EnsureUnloadedAsync(_currentGameplayScene);

            _currentGameplayScene = null;

            IsTransitioning = false;
        }

        public async Task EnterHubAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_hubScene);
            await EnsureUnloadedAsync(_startScene);

            await EnsureUnloadedAsync(_gameModuleScene);

            if (!string.IsNullOrWhiteSpace(_currentGameplayScene))
                await EnsureUnloadedAsync(_currentGameplayScene);

            _currentGameplayScene = null;

            IsTransitioning = false;
        }

        public Task ExitApplicationAsync()
        {
#if UNITY_EDITOR
            Debug.Log("[App] Quit requested (Editor).");
            return Task.CompletedTask;
#else
            Application.Quit();
            return Task.CompletedTask;
#endif
        }

        public async Task EnterGameModuleAsync(string gameId)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            var session = AppContext.Services.Resolve<AppSession>();
            session.SelectGame(gameId);

            await EnsureLoadedAsync(_hubScene);
            await EnsureLoadedAsync(_gameModuleScene);

            IsTransitioning = false;
        }

        public async Task ExitGameModuleAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_hubScene);
            await EnsureUnloadedAsync(_gameModuleScene);

            IsTransitioning = false;
        }

        public async Task StartGameplayAsync(string gameplaySceneName)
        {
            if (IsTransitioning) return;
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
                return;

            IsTransitioning = true;

            _currentGameplayScene = gameplaySceneName;

            await EnsureLoadedAsync(_currentGameplayScene);
            await EnsureUnloadedAsync(_gameModuleScene);

            IsTransitioning = false;
        }

        public async Task ReturnToGameModuleAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_gameModuleScene);

            if (!string.IsNullOrWhiteSpace(_currentGameplayScene))
            {
                await EnsureUnloadedAsync(_currentGameplayScene);
                _currentGameplayScene = null;
            }

            IsTransitioning = false;
        }

        public async Task ReturnToHubAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_hubScene);

            await EnsureUnloadedAsync(_gameModuleScene);

            if (!string.IsNullOrWhiteSpace(_currentGameplayScene))
                await EnsureUnloadedAsync(_currentGameplayScene);

            _currentGameplayScene = null;

            IsTransitioning = false;
        }

        private static async Task EnsureLoadedAsync(string sceneName)
        {
            if (IsLoaded(sceneName)) return;

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone)
                await Task.Yield();
        }

        private static async Task EnsureUnloadedAsync(string sceneName)
        {
            if (!IsLoaded(sceneName)) return;

            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone)
                await Task.Yield();
        }

        private static bool IsLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc.isLoaded && sc.name == sceneName)
                    return true;
            }
            return false;
        }
    }
}