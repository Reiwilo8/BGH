using System;

namespace Project.Core.Input
{
    public interface IInputService
    {
        event Action<NavAction> OnNavAction;

        event Action<NavDirection4> OnNavDirection4;

        void Emit(NavAction action);

        void EmitDirection4(NavDirection4 direction);
    }
}