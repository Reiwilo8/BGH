using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.App
{
    public sealed class AppFlowService : IAppFlowService
    {
        private readonly string _startScene;
        private readonly string _hubScene;

        public bool IsTransitioning { get; private set; }

        public AppFlowService(string startScene = "StartScene", string hubScene = "HubScene")
        {
            _startScene = startScene;
            _hubScene = hubScene;
        }

        public async Task EnterStartAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_startScene);
            await EnsureUnloadedAsync(_hubScene);

            IsTransitioning = false;
        }

        public async Task EnterHubAsync()
        {
            if (IsTransitioning) return;
            IsTransitioning = true;

            await EnsureLoadedAsync(_hubScene);
            await EnsureUnloadedAsync(_startScene);

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

        public Task EnterGameModuleAsync(string gameId)
        {
            Debug.Log($"[AppFlow] EnterGameModuleAsync({gameId}) - TODO");
            return Task.CompletedTask;
        }

        public Task ReturnToHubAsync() => EnterHubAsync();

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