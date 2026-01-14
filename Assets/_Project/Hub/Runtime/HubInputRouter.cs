using Project.Core.Activity;
using Project.Core.App;
using Project.Core.Input;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubInputRouter : MonoBehaviour
    {
        [SerializeField] private HubController hub;

        private IInputService _input;
        private IInputFocusService _focus;
        private IRepeatService _repeat;

        private void Awake()
        {
            if (hub == null)
                hub = FindFirstObjectByType<HubController>();

            _input = AppContext.Services.Resolve<IInputService>();
            _focus = AppContext.Services.Resolve<IInputFocusService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();
        }

        private void OnEnable()
        {
            _focus.Push(InputScope.Hub);

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

            _focus.Pop(InputScope.Hub);
        }

        private void HandleNav(NavAction action)
        {
            if (hub == null) return;
            if (_focus.Current != InputScope.Hub) return;

            if (action == NavAction.ToggleVisualAssist)
                return;

            hub.Handle(action);
        }

        private void HandleRepeat()
        {
            if (hub == null) return;
            if (_focus.Current != InputScope.Hub) return;

            hub.OnRepeatRequested();
        }
    }
}