using System;

namespace Project.Hub
{
    public sealed class HubTransitionGate
    {
        public bool IsTransitioning { get; private set; }

        public void RunInstant(Action action)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            try
            {
                action?.Invoke();
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public async System.Threading.Tasks.Task RunAsync(Func<System.Threading.Tasks.Task> action)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            try
            {
                if (action != null) await action();
            }
            finally
            {
                IsTransitioning = false;
            }
        }
    }
}