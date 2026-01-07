using System;

namespace Project.Core.Input
{
    public interface IInputService
    {
        event Action<NavAction> OnNavAction;
        void Emit(NavAction action);
    }
}