using UnityEngine;
using Project.Core.Visual;
using Project.Core.Input;

namespace Project.Core.Settings
{
    public sealed class PlayerPrefsSettingsService : ISettingsService
    {
        private const string Key = "app_settings_v2";

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
                return;
            }

            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                Current = Clone(Defaults);
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

            Current.repeatIdleSeconds = Mathf.Clamp(Current.repeatIdleSeconds, 1f, 15f);
            Current.sfxVolume = Mathf.Clamp01(Current.sfxVolume);
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(Current);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            Current = Clone(Defaults);
            Save();
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
            Save();
        }

        public void SetSfxVolume01(float volume01)
        {
            Current.sfxVolume = Mathf.Clamp01(volume01);
            Save();
        }

        public void SetCuesEnabled(bool enabled)
        {
            Current.cuesEnabled = enabled;
            Save();
        }

        private static AppSettingsData Clone(AppSettingsData src)
        {
            return new AppSettingsData
            {
                languageCode = src.languageCode,
                hasUserSelectedLanguage = src.hasUserSelectedLanguage,
                visualMode = src.visualMode,
                controlHintMode = src.controlHintMode,
                repeatIdleSeconds = src.repeatIdleSeconds,
                sfxVolume = src.sfxVolume,
                cuesEnabled = src.cuesEnabled
            };
        }
    }
}