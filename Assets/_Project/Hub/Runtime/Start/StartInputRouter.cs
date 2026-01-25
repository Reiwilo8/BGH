using Project.Core.Activity;
using Project.Core.App;
using Project.Core.Input;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Hub.Start
{
    public sealed class StartInputRouter : MonoBehaviour
    {
        [SerializeField] private StartController controller;

        private IInputService _input;
        private IInputFocusService _focus;
        private IRepeatService _repeat;
        private IVisualAssistService _va;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<StartController>();

            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();
            _va = AppContext.Services.Resolve<IVisualAssistService>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.Start);

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

            _focus.Pop(InputScope.Start);
        }

        private void HandleNav(NavAction action)
        {
            if (controller == null) return;
            if (_focus.Current != InputScope.Start) return;

            switch (action)
            {
                case NavAction.Confirm:
                    controller.EnterHub();
                    break;

                case NavAction.Back:
                    controller.ExitApp();
                    break;

                case NavAction.ToggleVisualAssist:
                    controller.ToggleVisualAssist();
                    break;

                default:
                    break;
            }
        }

        private void HandleRepeat()
        {
            if (controller == null) return;
            if (_focus.Current != InputScope.Start) return;

            _va?.FlashRepeat(0.25f);
            controller.OnRepeatRequested();
        }
    }
}