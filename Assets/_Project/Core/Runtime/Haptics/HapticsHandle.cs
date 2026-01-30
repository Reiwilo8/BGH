using System;

namespace Project.Core.Haptics
{
    public sealed class HapticsHandle
    {
        internal Action StopAction;

        public bool IsStopped { get; private set; }

        public void Stop()
        {
            if (IsStopped) return;
            IsStopped = true;
            StopAction?.Invoke();
            StopAction = null;
        }
    }
}