using System;
using Project.Core.Visual;
using Project.Core.Input;
using Project.Core.VisualAssist;

namespace Project.Core.Settings
{
    [Serializable]
    public sealed class AppSettingsData
    {
        public string languageCode = "en";
        public bool hasUserSelectedLanguage = false;

        public VisualMode visualMode = VisualMode.AudioOnly;

        public VisualAssistTextSizePreset vaTextSizePreset = VisualAssistTextSizePreset.Medium;
        public float vaMarqueeSpeedScale = 1f;
        public float vaDimmerStrength01 = 0.7f;

        public ControlHintMode controlHintMode = ControlHintMode.Auto;

        public float repeatIdleSeconds = 10f;

        public bool autoRepeatEnabled = true;
        public float autoRepeatIdleSeconds = 20f;

        public float sfxVolume = 1f;
        public bool cuesEnabled = true;
    }
}