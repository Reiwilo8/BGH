namespace Project.Core.Haptics
{
    internal interface IHapticsBackend
    {
        bool IsSupported { get; }
        void VibrateMilliseconds(long ms);
        void Cancel();
    }
}