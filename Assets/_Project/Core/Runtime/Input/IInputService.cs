using System;
using Project.Core.Input.Motion;

namespace Project.Core.Input
{
    public interface IInputService
    {
        event Action<NavAction> OnNavAction;
        event Action<NavDirection4> OnNavDirection4;
        event Action<MotionAction> OnMotionAction;

        void Emit(NavAction action);
        void EmitDirection4(NavDirection4 direction);
        void EmitMotion(MotionAction action);
    }
}