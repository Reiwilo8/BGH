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
#elif UNITY_STANDALONE || UNITY_EDITOR
            return ControlScheme.KeyboardMouse;
#else
            return ControlScheme.KeyboardMouse;
#endif
        }
    }
}