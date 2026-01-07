using Project.Core.App;
using Project.Core.Input;
using UnityEngine;

namespace Project.Hub.Start
{
    public sealed class StartInputRouter : MonoBehaviour
    {
        [SerializeField] private StartController controller;

        private IInputService _input;

        private void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<StartController>();

            _input = AppContext.Services.Resolve<IInputService>();
        }

        private void OnEnable()
        {
            if (_input != null)
                _input.OnNavAction += Handle;
        }

        private void OnDisable()
        {
            if (_input != null)
                _input.OnNavAction -= Handle;
        }

        private void Handle(NavAction action)
        {
            if (controller == null) return;

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
    }
}