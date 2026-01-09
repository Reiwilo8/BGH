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

        public ControlScheme preferredControlScheme = ControlScheme.Touch;
        public bool hasUserSelectedControlScheme = false;
    }
}