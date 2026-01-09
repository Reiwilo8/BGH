using System;
using System.Collections.Generic;

namespace Project.Core.Input
{
    public sealed class InputFocusService : IInputFocusService
    {
        private readonly List<InputScope> _stack = new();

        public InputScope Current { get; private set; } = InputScope.Start;
        public event Action<InputScope> OnChanged;

        public void Push(InputScope scope)
        {
            if (_stack.Count > 0 && _stack[^1] == scope)
                return;

            _stack.Add(scope);
            Set(scope);
        }

        public void Pop(InputScope scope)
        {
            if (_stack.Count == 0)
            {
                Set(InputScope.Start);
                return;
            }

            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i] != scope) continue;

                _stack.RemoveAt(i);
                break;
            }

            Set(_stack.Count > 0 ? _stack[^1] : InputScope.Start);
        }

        private void Set(InputScope scope)
        {
            if (Current == scope) return;
            Current = scope;
            OnChanged?.Invoke(Current);
        }
    }
}