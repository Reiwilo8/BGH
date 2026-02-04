using Project.Core.Activity;
using Project.Core.App;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Core.Input
{
    public sealed class KeyboardMouseInputSource : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actionsAsset;

        private IInputService _input;
        private IRepeatService _repeat;

        private InputActionMap _nav;
        private InputAction _next;
        private InputAction _prev;
        private InputAction _confirm;
        private InputAction _back;
        private InputAction _toggle;

        private InputAction _repeatAction;
        private InputAction _scroll;

        private InputAction _up;
        private InputAction _down;
        private InputAction _left;
        private InputAction _right;

        private float _nextScrollAllowedTime;
        private const float ScrollCooldownSeconds = 0.08f;
        private const float ScrollThreshold = 0.10f;

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

            _repeat = AppContext.Services.Resolve<IRepeatService>();

            _repeatAction = _nav.FindAction("Repeat", true);
            _scroll = _nav.FindAction("Scroll", true);

            _up = _nav.FindAction("Up", throwIfNotFound: false);
            _down = _nav.FindAction("Down", throwIfNotFound: false);
            _left = _nav.FindAction("Left", throwIfNotFound: false);
            _right = _nav.FindAction("Right", throwIfNotFound: false);
        }

        private void OnEnable()
        {
            _nav?.Enable();

            if (_next != null) _next.performed += OnNext;
            if (_prev != null) _prev.performed += OnPrev;
            if (_confirm != null) _confirm.performed += OnConfirm;
            if (_back != null) _back.performed += OnBack;
            if (_toggle != null) _toggle.performed += OnToggle;

            if (_repeatAction != null) _repeatAction.performed += OnRepeat;
            if (_scroll != null) _scroll.performed += OnScroll;

            if (_up != null) _up.performed += OnUp;
            if (_down != null) _down.performed += OnDown;
            if (_left != null) _left.performed += OnLeft;
            if (_right != null) _right.performed += OnRight;
        }

        private void OnDisable()
        {
            if (_next != null) _next.performed -= OnNext;
            if (_prev != null) _prev.performed -= OnPrev;
            if (_confirm != null) _confirm.performed -= OnConfirm;
            if (_back != null) _back.performed -= OnBack;
            if (_toggle != null) _toggle.performed -= OnToggle;

            if (_repeatAction != null) _repeatAction.performed -= OnRepeat;
            if (_scroll != null) _scroll.performed -= OnScroll;

            if (_up != null) _up.performed -= OnUp;
            if (_down != null) _down.performed -= OnDown;
            if (_left != null) _left.performed -= OnLeft;
            if (_right != null) _right.performed -= OnRight;

            _nav?.Disable();
        }

        private void OnNext(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Next);
        private void OnPrev(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Previous);
        private void OnConfirm(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Confirm);
        private void OnBack(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Back);
        private void OnToggle(InputAction.CallbackContext ctx) => _input.Emit(NavAction.ToggleVisualAssist);

        private void OnRepeat(InputAction.CallbackContext ctx) => _repeat.RequestRepeat("space");

        private void OnScroll(InputAction.CallbackContext ctx)
        {
            var now = Time.unscaledTime;
            if (now < _nextScrollAllowedTime) return;

            var v = ctx.ReadValue<Vector2>();
            var y = v.y;

            if (y > ScrollThreshold)
            {
                _input.Emit(NavAction.Previous);
                _nextScrollAllowedTime = now + ScrollCooldownSeconds;
            }
            else if (y < -ScrollThreshold)
            {
                _input.Emit(NavAction.Next);
                _nextScrollAllowedTime = now + ScrollCooldownSeconds;
            }
        }

        private void OnUp(InputAction.CallbackContext ctx) => _input.EmitDirection4(NavDirection4.Up);

        private void OnDown(InputAction.CallbackContext ctx) => _input.EmitDirection4(NavDirection4.Down);

        private void OnLeft(InputAction.CallbackContext ctx) => _input.EmitDirection4(NavDirection4.Left);

        private void OnRight(InputAction.CallbackContext ctx) => _input.EmitDirection4(NavDirection4.Right);
    }
}