using System;
using Project.Core.Activity;

namespace Project.Core.Input
{
    public sealed class InputService : IInputService
    {
        private readonly IUserInactivityService _inactivity;

        public event Action<NavAction> OnNavAction;

        public InputService(IUserInactivityService inactivity)
        {
            _inactivity = inactivity;
        }

        public void Emit(NavAction action)
        {
            _inactivity.MarkNavAction();
            OnNavAction?.Invoke(action);
        }
    }
}