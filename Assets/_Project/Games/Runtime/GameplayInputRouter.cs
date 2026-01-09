using Project.Core.App;
using Project.Core.Input;
using UnityEngine;

namespace Project.Games.Gameplay
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        [SerializeField] private GameplayPlaceholderController controller;

        private IInputService _input;
        private IInputFocusService _focus;

        private void Awake()
        {
            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();

            if (controller == null)
                controller = FindFirstObjectByType<GameplayPlaceholderController>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.Gameplay);
            _input.OnNavAction += Handle;
        }

        private void OnDisable()
        {
            _input.OnNavAction -= Handle;
            _focus.Pop(InputScope.Gameplay);
        }

        private void Handle(NavAction action)
        {
            if (_focus.Current != InputScope.Gameplay) return;

            if (action == NavAction.ToggleVisualAssist) return;
            controller?.Handle(action);
        }
    }
}