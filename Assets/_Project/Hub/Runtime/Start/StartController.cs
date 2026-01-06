using System.Collections;
using Project.Core.App;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.UI.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Hub.Start
{
    public sealed class StartController : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string startSceneName = "StartScene";
        [SerializeField] private string hubSceneName = "HubScene";

        [Header("Visual Assist UI (optional)")]
        [SerializeField] private StartVisualUiController visualUi;

        private IVisualModeService _visualMode;
        private ISpeechService _speech;
        private SpeechFeedRouter _speechFeedRouter;

        private bool _isTransitioning;

        private void Awake()
        {
            var services = App.Services;

            if (!services.TryResolve<IVisualModeService>(out _visualMode))
            {
                _visualMode = new VisualModeService();
                services.Register<IVisualModeService>(_visualMode);
            }

            _speech = services.Resolve<ISpeechService>();

            _speechFeedRouter = services.Resolve<SpeechFeedRouter>();
            if (visualUi != null)
                _speechFeedRouter.SetTarget(visualUi);
        }

        private void Start()
        {
            RefreshUi();

            _speech.Speak("Start. Confirm to open the Hub. Back to quit. Toggle Visual Assist available on this screen.", SpeechPriority.Normal);
        }

        private void OnDestroy()
        {
            if (_speechFeedRouter != null && visualUi != null)
                _speechFeedRouter.ClearTarget(visualUi);
        }

        public void EnterHub()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _speech.Speak("Opening Hub.", SpeechPriority.High);
            StartCoroutine(GoToHub());
        }

        public void ExitApp()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _speech.Speak("Quitting application.", SpeechPriority.High);
            QuitApp();
        }

        public void ToggleVisualAssist()
        {
            if (_isTransitioning) return;

            _visualMode.ToggleVisualAssist();
            RefreshUi();

            string msg = _visualMode.Mode == VisualMode.VisualAssist
                ? "Visual Assist enabled."
                : "Visual Assist disabled.";

            _speech.Speak(msg, SpeechPriority.High);
        }

        private void RefreshUi()
        {
            if (visualUi == null) return;

            visualUi.ApplyMode(_visualMode.Mode);
            visualUi.SetContent(BuildUiText());
        }

        private static string BuildUiText()
        {
            return
                "START\n\n" +
                "Keyboard / Mouse:\n" +
                "- Confirm: Enter / Space / Left Click\n" +
                "- Back/Pause: Backspace / Esc / Right Click\n" +
                "- Toggle Visual Assist: F1 (Start screen only)\n\n" +
                "Touch gestures:\n" +
                "- Confirm: Double-tap\n" +
                "- Back/Pause: Long-press\n" +
                "- Toggle Visual Assist: Two-finger tap (Start screen only)";
        }

        private IEnumerator GoToHub()
        {
            if (!IsSceneLoaded(hubSceneName))
            {
                var load = SceneManager.LoadSceneAsync(hubSceneName, LoadSceneMode.Additive);
                while (load != null && !load.isDone) yield return null;
            }

            if (IsSceneLoaded(startSceneName))
            {
                var unload = SceneManager.UnloadSceneAsync(startSceneName);
                while (unload != null && !unload.isDone) yield return null;
            }

            var hubScene = SceneManager.GetSceneByName(hubSceneName);
            if (hubScene.IsValid() && hubScene.isLoaded)
                SceneManager.SetActiveScene(hubScene);
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

        private static void QuitApp()
        {
#if UNITY_EDITOR
            Debug.Log("[App] Quit requested (Editor).");
#else
            Application.Quit();
#endif
        }
    }
}