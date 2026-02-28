using System;
using Project.Core.Activity;
using Project.Core.Input.Motion;

namespace Project.Core.Input
{
    public sealed class InputService : IInputService
    {
        private readonly IUserInactivityService _inactivity;

        public event Action<NavAction> OnNavAction;
        public event Action<NavDirection4> OnNavDirection4;
        public event Action<MotionAction> OnMotionAction;

        public InputService(IUserInactivityService inactivity)
        {
            _inactivity = inactivity;
        }

        public void Emit(NavAction action)
        {
            _inactivity.MarkNavAction();
            OnNavAction?.Invoke(action);
        }

        public void EmitDirection4(NavDirection4 direction)
        {
            _inactivity.MarkNavAction();
            OnNavDirection4?.Invoke(direction);
        }

        public void EmitMotion(MotionAction action)
        {
            _inactivity.MarkNavAction();
            OnMotionAction?.Invoke(action);
        }
    }
}