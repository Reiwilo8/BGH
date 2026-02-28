using Project.Core.Activity;
using Project.Core.App;
using Project.Core.Input.Motion;
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

        private InputAction _motionShake;
        private InputAction _motionUp;
        private InputAction _motionDown;
        private InputAction _motionTilt;

        private float _nextScrollAllowedTime;
        private const float ScrollCooldownSeconds = 0.08f;
        private const float ScrollThreshold = 0.10f;

        private float _tiltValue;
        private float _nextTiltAllowedTime;
        private const float TiltCooldownSeconds = 0.05f;
        private const float TiltDeadzone = 0.10f;

        private void Awake()
        {
            if (actionsAsset == null)
            {
                Debug.LogError("[Input] actionsAsset is not assigned on KeyboardMouseInputSource.");
                enabled = false;
                return;
            }

            _input = AppContext.Services.Resolve<IInputService>();
            _repeat = AppContext.Services.Resolve<IRepeatService>();

            _nav = actionsAsset.FindActionMap("Navigation", throwIfNotFound: true);

            _next = _nav.FindAction("Next", true);
            _prev = _nav.FindAction("Previous", true);
            _confirm = _nav.FindAction("Confirm", true);
            _back = _nav.FindAction("Back", true);
            _toggle = _nav.FindAction("ToggleVisualAssist", true);

            _repeatAction = _nav.FindAction("Repeat", true);
            _scroll = _nav.FindAction("Scroll", true);

            _up = _nav.FindAction("Up", throwIfNotFound: false);
            _down = _nav.FindAction("Down", throwIfNotFound: false);
            _left = _nav.FindAction("Left", throwIfNotFound: false);
            _right = _nav.FindAction("Right", throwIfNotFound: false);

            _motionShake = _nav.FindAction("MotionShake", throwIfNotFound: false);
            _motionUp = _nav.FindAction("MotionUp", throwIfNotFound: false);
            _motionDown = _nav.FindAction("MotionDown", throwIfNotFound: false);
            _motionTilt = _nav.FindAction("MotionTilt", throwIfNotFound: false);
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

            if (_motionShake != null) _motionShake.performed += OnMotionShake;
            if (_motionUp != null) _motionUp.performed += OnMotionUp;
            if (_motionDown != null) _motionDown.performed += OnMotionDown;

            if (_motionTilt != null)
            {
                _motionTilt.performed += OnMotionTilt;
                _motionTilt.canceled += OnMotionTilt;
            }
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

            if (_motionShake != null) _motionShake.performed -= OnMotionShake;
            if (_motionUp != null) _motionUp.performed -= OnMotionUp;
            if (_motionDown != null) _motionDown.performed -= OnMotionDown;

            if (_motionTilt != null)
            {
                _motionTilt.performed -= OnMotionTilt;
                _motionTilt.canceled -= OnMotionTilt;
            }

            _nav?.Disable();
        }

        private void Update()
        {
            if (_motionTilt == null) return;

            if (Mathf.Abs(_tiltValue) < TiltDeadzone) return;

            var now = Time.unscaledTime;
            if (now < _nextTiltAllowedTime) return;

            if (_tiltValue < 0f) _input.EmitMotion(MotionAction.TiltLeft);
            else _input.EmitMotion(MotionAction.TiltRight);

            _nextTiltAllowedTime = now + TiltCooldownSeconds;
        }

        private void OnNext(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Next);
        private void OnPrev(InputAction.CallbackContext ctx) => _input.Emit(NavAction.Previous);

        private void OnConfirm(InputAction.CallbackContext ctx)
        {
            _input.Emit(NavAction.Confirm);

            if (_motionShake == null)
                _input.EmitMotion(MotionAction.Shake);
        }

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
                _input.EmitMotion(MotionAction.Up);

                _nextScrollAllowedTime = now + ScrollCooldownSeconds;
            }
            else if (y < -ScrollThreshold)
            {
                _input.Emit(NavAction.Next);
                _input.EmitMotion(MotionAction.Down);

                _nextScrollAllowedTime = now + ScrollCooldownSeconds;
            }
        }

        private void OnUp(InputAction.CallbackContext ctx)
        {
            _input.EmitDirection4(NavDirection4.Up);

            if (_motionUp == null)
                _input.EmitMotion(MotionAction.Up);
        }

        private void OnDown(InputAction.CallbackContext ctx)
        {
            _input.EmitDirection4(NavDirection4.Down);

            if (_motionDown == null)
                _input.EmitMotion(MotionAction.Down);
        }

        private void OnLeft(InputAction.CallbackContext ctx)
        {
            _input.EmitDirection4(NavDirection4.Left);

            if (_motionTilt == null)
                _input.EmitMotion(MotionAction.TiltLeft);
        }

        private void OnRight(InputAction.CallbackContext ctx)
        {
            _input.EmitDirection4(NavDirection4.Right);

            if (_motionTilt == null)
                _input.EmitMotion(MotionAction.TiltRight);
        }

        private void OnMotionShake(InputAction.CallbackContext ctx)
            => _input.EmitMotion(MotionAction.Shake);

        private void OnMotionUp(InputAction.CallbackContext ctx)
            => _input.EmitMotion(MotionAction.Up);

        private void OnMotionDown(InputAction.CallbackContext ctx)
            => _input.EmitMotion(MotionAction.Down);

        private void OnMotionTilt(InputAction.CallbackContext ctx)
        {
            _tiltValue = ctx.ReadValue<float>();
            if (Mathf.Abs(_tiltValue) >= TiltDeadzone)
                _nextTiltAllowedTime = 0f;
        }
    }
}