using UnityEngine;
using Project.Core.Input;

namespace Project.Core.Settings
{
    public static class StartupDefaultsResolver
    {
        public static string ResolveSystemLanguageCode()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Polish => "pl",
                SystemLanguage.English => "en",
                _ => "en"
            };
        }

        public static ControlScheme ResolvePlatformPreferredControlScheme()
        {
#if UNITY_ANDROID || UNITY_IOS
            return ControlScheme.Touch;
#else
            return ControlScheme.KeyboardMouse;
#endif
        }

        public static ControlHintMode ResolvePlatformPreferredHintMode()
        {
            return ResolvePlatformPreferredControlScheme() == ControlScheme.Touch
                ? ControlHintMode.Touch
                : ControlHintMode.KeyboardMouse;
        }
    }
}