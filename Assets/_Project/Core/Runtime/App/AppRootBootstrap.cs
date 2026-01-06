using System.Collections;
using Project.Core.Services;
using Project.Core.Speech;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.App
{
    public sealed class AppRootBootstrap : MonoBehaviour
    {
        [Header("Additive scenes")]
        [SerializeField] private string startSceneName = "StartScene";

        private IServiceRegistry _services;

        public IServiceRegistry Services => _services;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _services = new ServiceRegistry();

            var feedRouter = new SpeechFeedRouter();
            _services.Register(feedRouter);

            var speech = SpeechServiceFactory.Create(feedRouter);
            _services.Register<ISpeechService>(speech);

            StartCoroutine(BootRoutine());
        }

        private IEnumerator BootRoutine()
        {
            if (!string.IsNullOrWhiteSpace(startSceneName) && !IsSceneLoaded(startSceneName))
            {
                var op = SceneManager.LoadSceneAsync(startSceneName, LoadSceneMode.Additive);
                while (op != null && !op.isDone)
                    yield return null;
            }
        }

        private static bool IsSceneLoaded(string sceneName)
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