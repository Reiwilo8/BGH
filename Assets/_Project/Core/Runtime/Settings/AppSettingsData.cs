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

        public ControlHintMode controlHintMode = ControlHintMode.Auto;

        public bool cuesEnabled = true;
        public float cuesVolume = 1f;
        public float gameVolume = 1f;

        public float repeatIdleSeconds = 10f;

        public bool autoRepeatEnabled = true;
        public float autoRepeatIdleSeconds = 20f;

        public VisualMode visualMode = VisualMode.AudioOnly;

        public VisualAssistTextSizePreset vaTextSizePreset = VisualAssistTextSizePreset.Medium;
        public float vaMarqueeSpeedScale = 1f;
        public float vaDimmerStrength01 = 0.7f;
    }
}