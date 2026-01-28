using System;
using UnityEngine;
using Project.Core.Visual;
using Project.Core.Input;
using Project.Core.VisualAssist;

namespace Project.Core.Settings
{
    public sealed class PlayerPrefsSettingsService : ISettingsService
    {
        private const string Key = "app_settings_v3";

        private const string VaInitMarkerKey = "app_settings_v3_va_init";
        private const string RepeatInitMarkerKey = "app_settings_v3_repeat_init";

        public event Action Changed;

        public AppSettingsData Current { get; private set; }
        public AppSettingsData Defaults { get; }

        public PlayerPrefsSettingsService(AppSettingsData defaults)
        {
            Defaults = Clone(defaults);
            Current = Clone(defaults);
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                Current = Clone(Defaults);
                PostLoadNormalizeAndMigrate();
                return;
            }

            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                Current = Clone(Defaults);
                PostLoadNormalizeAndMigrate();
                return;
            }

            try
            {
                var loaded = JsonUtility.FromJson<AppSettingsData>(json);
                Current = loaded ?? Clone(Defaults);
            }
            catch
            {
                Current = Clone(Defaults);
            }

            PostLoadNormalizeAndMigrate();
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(Current);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();

            Changed?.Invoke();
        }

        public void ResetToDefaults()
        {
            Current = Clone(Defaults);

            Current.hasUserSelectedLanguage = false;

            ApplyAutomaticDefaultsAfterReset();
            NormalizeAfterReset();

            Save();
        }

        private void ApplyAutomaticDefaultsAfterReset()
        {
            var sys = StartupDefaultsResolver.ResolveSystemLanguageCode();
            if (string.IsNullOrWhiteSpace(sys))
                sys = Defaults.languageCode;

            if (string.IsNullOrWhiteSpace(sys))
                sys = "en";

            Current.languageCode = sys;

            if (string.IsNullOrWhiteSpace(Current.languageCode))
                Current.languageCode = "en";
        }

        private void NormalizeAfterReset()
        {
            Current.cuesVolume = Mathf.Clamp01(Current.cuesVolume);
            Current.gameVolume = Mathf.Clamp01(Current.gameVolume);

            Current.repeatIdleSeconds = Mathf.Clamp(Current.repeatIdleSeconds, 1f, 15f);

            if (!Enum.IsDefined(typeof(VisualAssistTextSizePreset), Current.vaTextSizePreset))
                Current.vaTextSizePreset = Defaults.vaTextSizePreset;

            Current.vaMarqueeSpeedScale = Mathf.Clamp(Current.vaMarqueeSpeedScale, 0.5f, 2.0f);
            Current.vaMarqueeSpeedScale = Quantize(Current.vaMarqueeSpeedScale, 0.1f);

            Current.vaDimmerStrength01 = Mathf.Clamp01(Current.vaDimmerStrength01);
            Current.vaDimmerStrength01 = Quantize(Current.vaDimmerStrength01, 0.1f);

            NormalizeRepeatCoherence();
        }

        public void SetLanguage(string languageCode, bool userSelected)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
                Current.languageCode = languageCode;

            Current.hasUserSelectedLanguage = userSelected;
            Save();
        }

        public void SetVisualMode(VisualMode mode)
        {
            Current.visualMode = mode;
            Save();
        }

        public void SetControlHintMode(ControlHintMode mode)
        {
            Current.controlHintMode = mode;
            Save();
        }

        public void SetRepeatIdleSeconds(float seconds)
        {
            Current.repeatIdleSeconds = Mathf.Clamp(seconds, 1f, 15f);

            float minAuto = Mathf.Max(10f, Current.repeatIdleSeconds);
            Current.autoRepeatIdleSeconds = Mathf.Clamp(Current.autoRepeatIdleSeconds, 10f, 30f);
            if (Current.autoRepeatIdleSeconds < minAuto)
                Current.autoRepeatIdleSeconds = minAuto;

            Save();
        }

        public void SetAutoRepeatEnabled(bool enabled)
        {
            Current.autoRepeatEnabled = enabled;
            Save();
        }

        public void SetAutoRepeatIdleSeconds(float seconds)
        {
            seconds = Mathf.Clamp(seconds, 10f, 30f);

            float minAuto = Mathf.Max(10f, Current.repeatIdleSeconds);
            if (seconds < minAuto)
                seconds = minAuto;

            Current.autoRepeatIdleSeconds = seconds;
            Save();
        }

        public void SetCuesVolume01(float volume01)
        {
            Current.cuesVolume = Mathf.Clamp01(volume01);
            Save();
        }

        public void SetGameVolume01(float volume01)
        {
            Current.gameVolume = Mathf.Clamp01(volume01);
            Save();
        }

        public void SetCuesEnabled(bool enabled)
        {
            Current.cuesEnabled = enabled;
            Save();
        }

        public void SetVaTextSizePreset(VisualAssistTextSizePreset preset)
        {
            Current.vaTextSizePreset = preset;
            Save();
        }

        public void SetVaMarqueeSpeedScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.5f, 2.0f);
            scale = Quantize(scale, 0.1f);
            Current.vaMarqueeSpeedScale = scale;
            Save();
        }

        public void SetVaDimmerStrength01(float strength01)
        {
            strength01 = Mathf.Clamp01(strength01);
            strength01 = Quantize(strength01, 0.1f);
            Current.vaDimmerStrength01 = strength01;
            Save();
        }

        private void PostLoadNormalizeAndMigrate()
        {
            Current.cuesVolume = Mathf.Clamp01(Current.cuesVolume);
            Current.gameVolume = Mathf.Clamp01(Current.gameVolume);

            Current.repeatIdleSeconds = Mathf.Clamp(Current.repeatIdleSeconds, 1f, 15f);

            if (!Enum.IsDefined(typeof(VisualAssistTextSizePreset), Current.vaTextSizePreset))
                Current.vaTextSizePreset = Defaults.vaTextSizePreset;

            Current.vaMarqueeSpeedScale = Mathf.Clamp(Current.vaMarqueeSpeedScale, 0.5f, 2.0f);
            Current.vaMarqueeSpeedScale = Quantize(Current.vaMarqueeSpeedScale, 0.1f);

            Current.vaDimmerStrength01 = Mathf.Clamp01(Current.vaDimmerStrength01);
            Current.vaDimmerStrength01 = Quantize(Current.vaDimmerStrength01, 0.1f);

            if (!PlayerPrefs.HasKey(RepeatInitMarkerKey))
            {
                Current.autoRepeatEnabled = Defaults.autoRepeatEnabled;
                Current.autoRepeatIdleSeconds = Defaults.autoRepeatIdleSeconds;

                PlayerPrefs.SetInt(RepeatInitMarkerKey, 1);
                PlayerPrefs.Save();

                NormalizeRepeatCoherence();
                Save();
                return;
            }

            NormalizeRepeatCoherence();

            if (!PlayerPrefs.HasKey(VaInitMarkerKey))
            {
                Current.vaTextSizePreset = Defaults.vaTextSizePreset;

                Current.vaMarqueeSpeedScale = Defaults.vaMarqueeSpeedScale;
                Current.vaDimmerStrength01 = Defaults.vaDimmerStrength01;

                PlayerPrefs.SetInt(VaInitMarkerKey, 1);
                PlayerPrefs.Save();

                Save();
            }
        }

        private void NormalizeRepeatCoherence()
        {
            if (Current.autoRepeatIdleSeconds <= 0.0001f)
                Current.autoRepeatIdleSeconds = Defaults.autoRepeatIdleSeconds;

            Current.autoRepeatIdleSeconds = Mathf.Clamp(Current.autoRepeatIdleSeconds, 10f, 30f);

            float minAuto = Mathf.Max(10f, Current.repeatIdleSeconds);
            if (Current.autoRepeatIdleSeconds < minAuto)
                Current.autoRepeatIdleSeconds = minAuto;
        }

        private static float Quantize(float value, float step)
        {
            if (step <= 0.00001f) return value;
            return Mathf.Round(value / step) * step;
        }

        private static AppSettingsData Clone(AppSettingsData src)
        {
            return new AppSettingsData
            {
                languageCode = src.languageCode,
                hasUserSelectedLanguage = src.hasUserSelectedLanguage,

                controlHintMode = src.controlHintMode,

                cuesEnabled = src.cuesEnabled,
                cuesVolume = src.cuesVolume,
                gameVolume = src.gameVolume,

                repeatIdleSeconds = src.repeatIdleSeconds,

                autoRepeatEnabled = src.autoRepeatEnabled,
                autoRepeatIdleSeconds = src.autoRepeatIdleSeconds,

                visualMode = src.visualMode,

                vaTextSizePreset = src.vaTextSizePreset,
                vaMarqueeSpeedScale = src.vaMarqueeSpeedScale,
                vaDimmerStrength01 = src.vaDimmerStrength01
            };
        }
    }
}