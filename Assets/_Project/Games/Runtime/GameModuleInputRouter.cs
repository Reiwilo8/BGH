using Project.Core.App;
using Project.Core.Input;
using UnityEngine;

namespace Project.Games.Module
{
    public sealed class GameModuleInputRouter : MonoBehaviour
    {
        [SerializeField] private GameModuleController controller;

        private IInputService _input;
        private IInputFocusService _focus;

        private void Awake()
        {
            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();

            if (controller == null)
                controller = FindFirstObjectByType<GameModuleController>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.GameModule);
            _input.OnNavAction += Handle;
        }

        private void OnDisable()
        {
            _input.OnNavAction -= Handle;
            _focus.Pop(InputScope.GameModule);
        }

        private void Handle(NavAction action)
        {
            if (_focus.Current != InputScope.GameModule) return;

            if (action == NavAction.ToggleVisualAssist) return;
            controller?.Handle(action);
        }
    }
}