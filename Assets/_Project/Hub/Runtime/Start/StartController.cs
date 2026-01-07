using Project.Core.App;
using Project.Core.Speech;
using Project.Core.Visual;
using Project.UI.Visual;
using UnityEngine;

namespace Project.Hub.Start
{
    public sealed class StartController : MonoBehaviour
    {
        [Header("Visual Assist UI (optional)")]
        [SerializeField] private StartVisualUiController visualUi;

        private IVisualModeService _visualMode;
        private ISpeechService _speech;
        private SpeechFeedRouter _speechFeedRouter;
        private IAppFlowService _flow;

        private bool _isTransitioning;

        private void Awake()
        {
            var services = AppContext.Services;

            _visualMode = services.Resolve<IVisualModeService>();
            _speech = services.Resolve<ISpeechService>();
            _flow = services.Resolve<IAppFlowService>();

            _speechFeedRouter = services.Resolve<SpeechFeedRouter>();
            if (visualUi != null)
                _speechFeedRouter.SetTarget(visualUi);
        }

        private void Start()
        {
            RefreshUi();
            _speech.Speak(
                "Start. Confirm to open the Hub. Back to quit. Toggle Visual Assist is available on this screen.",
                SpeechPriority.Normal);
        }

        private void OnDestroy()
        {
            if (_speechFeedRouter != null && visualUi != null)
                _speechFeedRouter.ClearTarget(visualUi);
        }

        public async void EnterHub()
        {
            if (_isTransitioning || _flow.IsTransitioning) return;
            _isTransitioning = true;

            try
            {
                _speech.Speak("Opening Hub.", SpeechPriority.High);
                await _flow.EnterHubAsync();
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public async void ExitApp()
        {
            if (_isTransitioning || _flow.IsTransitioning) return;
            _isTransitioning = true;

            try
            {
                _speech.Speak("Quitting application.", SpeechPriority.High);
                await _flow.ExitApplicationAsync();
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void ToggleVisualAssist()
        {
            if (_isTransitioning || (_flow != null && _flow.IsTransitioning)) return;

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
    }
}