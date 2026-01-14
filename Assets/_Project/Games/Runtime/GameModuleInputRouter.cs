using Project.Core.Activity;
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
        private IRepeatService _repeat;

        private void Awake()
        {
            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();

            if (controller == null)
                controller = FindFirstObjectByType<GameModuleController>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.GameModule);

            if (_input != null)
                _input.OnNavAction += HandleNav;

            if (_repeat != null)
                _repeat.RepeatRequested += HandleRepeat;
        }

        private void OnDisable()
        {
            if (_input != null)
                _input.OnNavAction -= HandleNav;

            if (_repeat != null)
                _repeat.RepeatRequested -= HandleRepeat;

            _focus.Pop(InputScope.GameModule);
        }

        private void HandleNav(NavAction action)
        {
            if (_focus.Current != InputScope.GameModule) return;

            if (action == NavAction.ToggleVisualAssist) return;
            controller?.Handle(action);
        }

        private void HandleRepeat()
        {
            if (_focus.Current != InputScope.GameModule) return;
            controller?.OnRepeatRequested();
        }
    }
}