using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;
using UnityEngine;

namespace Project.Games.Gameplay
{
    public sealed class GameplayPlaceholderController : MonoBehaviour
    {
        private ISpeechService _speech;
        private IAppFlowService _flow;

        private void Awake()
        {
            _speech = AppContext.Services.Resolve<ISpeechService>();
            _flow = AppContext.Services.Resolve<IAppFlowService>();
        }

        private void Start()
        {
            _speech.Speak("Gameplay placeholder. Press Back to return to the game menu.", SpeechPriority.Normal);
        }

        public void Handle(NavAction action)
        {
            if (action != NavAction.Back) return;
            _ = ReturnAsync();
        }

        private async System.Threading.Tasks.Task ReturnAsync()
        {
            if (_flow.IsTransitioning) return;
            _speech.Speak("Returning to the game menu.", SpeechPriority.High);
            await _flow.ReturnToGameModuleAsync();
        }
    }
}