using System;

namespace Project.Core.Input
{
    public interface IInputFocusService
    {
        InputScope Current { get; }
        event Action<InputScope> OnChanged;

        void Push(InputScope scope);
        void Pop(InputScope scope);
    }
}