namespace Project.Core.Haptics
{
    public interface IHapticsService
    {
        bool IsHardwareSupported { get; }

        bool Enabled { get; }
        float StrengthScale01 { get; }
        bool AudioFallbackEnabled { get; }

        void SetEnabled(bool enabled);
        void SetStrengthScale01(float scale01);
        void SetAudioFallbackEnabled(bool enabled);

        void Pulse(HapticLevel level, float durationSeconds = -1f);

        HapticsHandle StartContinuous(HapticLevel level, float onSeconds = 0.06f, float offSeconds = 0.08f);

        void StopAll();
    }
}