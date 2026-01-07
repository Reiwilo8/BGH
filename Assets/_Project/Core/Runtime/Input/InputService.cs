using System;

namespace Project.Core.Input
{
    public sealed class InputService : IInputService
    {
        public event Action<NavAction> OnNavAction;

        public void Emit(NavAction action) => OnNavAction?.Invoke(action);
    }
}