using Project.Core.Activity;
using Project.Core.App;
using Project.Core.Input;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Games.Gameplay
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        [SerializeField] private GameplayPlaceholderController controller;

        private IInputService _input;
        private IInputFocusService _focus;
        private IRepeatService _repeat;
        private IVisualAssistService _va;

        private void Awake()
        {
            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();
            _va = AppContext.Services.Resolve<IVisualAssistService>();

            if (controller == null)
                controller = FindFirstObjectByType<GameplayPlaceholderController>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.Gameplay);

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

            _focus.Pop(InputScope.Gameplay);
        }

        private void HandleNav(NavAction action)
        {
            if (_focus.Current != InputScope.Gameplay) return;

            if (action == NavAction.ToggleVisualAssist) return;
            controller?.Handle(action);
        }

        private void HandleRepeat()
        {
            if (_focus.Current != InputScope.Gameplay) return;
            _va?.FlashRepeat(0.25f);
            controller?.OnRepeatRequested();
        }
    }
}