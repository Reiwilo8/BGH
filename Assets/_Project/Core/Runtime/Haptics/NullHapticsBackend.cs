namespace Project.Core.Haptics
{
    internal sealed class NullHapticsBackend : IHapticsBackend
    {
        public bool IsSupported => false;
        public void VibrateMilliseconds(long ms) { }
        public void Cancel() { }
    }
}