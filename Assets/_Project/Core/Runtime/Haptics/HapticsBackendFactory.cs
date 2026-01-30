namespace Project.Core.Haptics
{
    internal static class HapticsBackendFactory
    {
        public static IHapticsBackend Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHapticsBackend();
#elif UNITY_IOS && !UNITY_EDITOR
            return new IosHapticsBackend();
#else
            return new NullHapticsBackend();
#endif
        }
    }
}