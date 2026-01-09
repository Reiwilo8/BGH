using UnityEngine;
using Project.Core.Visual;
using Project.Core.Input;

namespace Project.Core.Settings
{
    public sealed class PlayerPrefsSettingsService : ISettingsService
    {
        private const string Key = "app_settings_v1";

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

        public void SetPreferredControlScheme(ControlScheme scheme, bool userSelected)
        {
            Current.preferredControlScheme = scheme;
            Current.hasUserSelectedControlScheme = userSelected;
            Save();
        }

        private static AppSettingsData Clone(AppSettingsData src)
        {
            return new AppSettingsData
            {
                languageCode = src.languageCode,
                hasUserSelectedLanguage = src.hasUserSelectedLanguage,
                visualMode = src.visualMode,
                preferredControlScheme = src.preferredControlScheme,
                hasUserSelectedControlScheme = src.hasUserSelectedControlScheme
            };
        }
    }
}