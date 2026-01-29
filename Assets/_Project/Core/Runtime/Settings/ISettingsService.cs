using System;
using Project.Core.Input;
using Project.Core.Visual;
using Project.Core.VisualAssist;

namespace Project.Core.Settings
{
    public interface ISettingsService
    {
        event Action Changed;

        AppSettingsData Current { get; }
        AppSettingsData Defaults { get; }

        void Load();
        void Save();

        void ResetToDefaults();

        void SetLanguage(string languageCode, bool userSelected);
        void SetVisualMode(VisualMode mode);

        void SetControlHintMode(ControlHintMode mode);

        void SetCuesEnabled(bool enabled);
        void SetCuesVolume01(float volume01);
        void SetGameVolume01(float volume01);

        void SetHapticsEnabled(bool enabled);
        void SetHapticsStrengthScale01(float scale01);
        void SetHapticsAudioFallbackEnabled(bool enabled);

        void SetRepeatIdleSeconds(float seconds);

        void SetAutoRepeatEnabled(bool enabled);
        void SetAutoRepeatIdleSeconds(float seconds);

        void SetVaTextSizePreset(VisualAssistTextSizePreset preset);
        void SetVaMarqueeSpeedScale(float scale);
        void SetVaDimmerStrength01(float strength01);
    }
}