using Project.Core.App;
using Project.Core.Input;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubInputRouter : MonoBehaviour
    {
        [SerializeField] private HubController hub;

        private IInputService _input;

        private void Awake()
        {
            if (hub == null)
                hub = FindFirstObjectByType<HubController>();

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
            if (hub == null) return;

            if (action == NavAction.ToggleVisualAssist)
                return;

            hub.Handle(action);
        }
    }
}