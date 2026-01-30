#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace Project.Core.Haptics
{
    internal sealed class AndroidHapticsBackend : IHapticsBackend
    {
        private AndroidJavaObject _vibrator;
        private int _sdkInt = -1;
        private bool _initialized;

        public bool IsSupported
        {
            get
            {
                EnsureInit();
                if (_vibrator == null) return false;

                try
                {
                    // hasVibrator() exists long time.
                    return _vibrator.Call<bool>("hasVibrator");
                }
                catch
                {
                    // If call fails, assume false.
                    return false;
                }
            }
        }

        public void VibrateMilliseconds(long ms)
        {
            EnsureInit();
            if (_vibrator == null) return;
            if (ms <= 0) return;

            try
            {
                // API 26+: use VibrationEffect if possible, but keep it amplitude-agnostic.
                if (_sdkInt >= 26)
                {
                    using var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    int defaultAmp = vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE");

                    using var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        ms,
                        defaultAmp
                    );

                    _vibrator.Call("vibrate", effect);
                }
                else
                {
#pragma warning disable CS0618
                    _vibrator.Call("vibrate", ms);
#pragma warning restore CS0618
                }
            }
            catch
            {
                // Last resort: try Unity vibrate
                try { Handheld.Vibrate(); } catch { }
            }
        }

        public void Cancel()
        {
            EnsureInit();
            if (_vibrator == null) return;

            try { _vibrator.Call("cancel"); }
            catch { }
        }

        private void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdkInt = version.GetStatic<int>("SDK_INT");

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null) return;

                // Context.VIBRATOR_SERVICE == "vibrator"
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch
            {
                _vibrator = null;
            }
        }
    }
}
#endif