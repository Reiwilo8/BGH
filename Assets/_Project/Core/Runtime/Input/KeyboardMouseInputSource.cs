using Project.Core.App;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Core.Input
{
    public sealed class KeyboardMouseInputSource : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actionsAsset;

        private IInputService _input;

        private InputActionMap _nav;
        private InputAction _next;
        private InputAction _prev;
        private InputAction _confirm;
        private InputAction _back;
        private InputAction _toggle;

        private void Awake()
        {
            if (actionsAsset == null)
            {
                Debug.LogError("[Input] actionsAsset is not assigned on KeyboardMouseInputSource.");
                enabled = false;
                return;
            }

            _input = AppContext.Services.Resolve<IInputService>();

            _nav = actionsAsset.FindActionMap("Navigation", throwIfNotFound: true);

            _next = _nav.FindAction("Next", true);
            _prev = _nav.FindAction("Previous", true);
            _confirm = _nav.FindAction("Confirm", true);
            _back = _nav.FindAction("Back", true);
            _toggle = _nav.FindAction("ToggleVisualAssist", true);
        }

        private void OnEnable()
        {
            _nav?.Enable();

            if (_next != null) _next.performed += OnNext;
            if (_prev != null) _prev.performed += OnPrev;
            if (_confirm != null) _confirm.performed += OnConfirm;
            if (_back != null) _back.performed += OnBack;
            if (_toggle != null) _toggle.performed += OnToggle;
        }

        private void OnDisable()
        {
            if (_next != null) _next.performed -= OnNext;
            if (_prev != null) _prev.performed -= OnPrev;
            if (_confirm != null) _confirm.performed -= OnConfirm;
            if (_back != null) _back.performed -= OnBack;
            if (_toggle != null) _toggle.performed -= OnToggle;

            _nav?.Disable();
        }

        private void OnNext(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Next);
        private void OnPrev(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Previous);
        private void OnConfirm(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Confirm);
        private void OnBack(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Back);
        private void OnToggle(InputAction.CallbackContext ctx) => _input.Emit(NavAction.ToggleVisualAssist);
    }
}