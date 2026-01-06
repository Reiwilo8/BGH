using System.Collections;
using Project.Core.App;
using Project.Core.Services;
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
        private StartOption _current = StartOption.EnterHub;

        private void Awake()
        {
            IServiceRegistry services = App.Services;

            if (!services.TryResolve<IVisualModeService>(out _visualMode))
            {
                _visualMode = new VisualModeService();
                services.Register<IVisualModeService>(_visualMode);
            }
        }

        private void Start()
        {
            RefreshUi();
            Announce();
        }

        private void RefreshUi()
        {
            visualUi?.ApplyMode(_visualMode.Mode);
            visualUi?.SetContent(BuildUiText());
        }

        private string BuildUiText()
        {
            return
                "START\n\n" +
                $"Selected: {_current}\n\n" +
                "Keyboard / Mouse:\n" +
                "- Next: Right or Down (arrows)\n" +
                "- Previous: Left or Up (arrows)\n" +
                "- Confirm: Enter / Space / Left Click\n" +
                "- Back/Pause: Backspace / Escape / Right Click\n" +
                "- Toggle Visual Assist: F1 (Start screen only)\n\n" +
                "Touch gestures:\n" +
                "- Next: Swipe Left or Swipe Up\n" +
                "- Previous: Swipe Right or Swipe Down\n" +
                "- Confirm: Double-tap\n" +
                "- Back/Pause: Long-press\n" +
                "- Toggle Visual Assist: Two-finger tap (Start screen only)\n";
        }

        private void Announce()
        {
            Debug.Log($"[Start] Selected option = {_current}");
        }

        public void Next()
        {
            _current = (StartOption)(((int)_current + 1) % 3);
            RefreshUi();
            Announce();
        }

        public void Previous()
        {
            _current = (StartOption)(((int)_current - 1 + 3) % 3);
            RefreshUi();
            Announce();
        }

        public void Confirm()
        {
            switch (_current)
            {
                case StartOption.EnterHub:
                    StartCoroutine(GoToHub());
                    break;

                case StartOption.ToggleVisualAssist:
                    ToggleVisualAssist();
                    break;

                case StartOption.Exit:
                    QuitApp();
                    break;
            }
        }

        public void Back()
        {
            QuitApp();
        }

        public void ToggleVisualAssist()
        {
            _visualMode.ToggleVisualAssist();
            RefreshUi();
            Announce();
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