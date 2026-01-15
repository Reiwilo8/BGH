using System;
using Project.Core.Visual;
using Project.Core.Input;

namespace Project.Core.Settings
{
    [Serializable]
    public sealed class AppSettingsData
    {
        public string languageCode = "en";
        public bool hasUserSelectedLanguage = false;

        public VisualMode visualMode = VisualMode.AudioOnly;

        public ControlHintMode controlHintMode = ControlHintMode.Auto;

        public float repeatIdleSeconds = 10f;

        public float sfxVolume = 1f;
        public bool cuesEnabled = true;
    }
}