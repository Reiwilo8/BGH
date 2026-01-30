#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine;

namespace Project.Core.Haptics
{
    internal sealed class IosHapticsBackend : IHapticsBackend
    {
        public bool IsSupported => true;

        public void VibrateMilliseconds(long ms)
        {
            Handheld.Vibrate();
        }

        public void Cancel()
        {
        }
    }
}
#endif