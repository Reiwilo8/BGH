namespace Project.Core.Audio
{
    public sealed class UiAudioSequenceHandle
    {
        public bool IsCancelled { get; private set; }
        public bool IsCompleted { get; private set; }

        public void Cancel()
        {
            IsCancelled = true;
        }

        internal void MarkCompleted()
        {
            IsCompleted = true;
        }
    }
}