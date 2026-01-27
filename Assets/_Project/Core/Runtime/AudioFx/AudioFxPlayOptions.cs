using UnityEngine;

namespace Project.Core.AudioFx
{
    public struct AudioFxPlayOptions
    {
        public float Volume01;
        public float Pitch;
        public float PanStereo;
        public bool Loop;

        public float StartTimeSeconds;
        public float EndTimeSeconds;

        public static AudioFxPlayOptions Default => new AudioFxPlayOptions
        {
            Volume01 = 1f,
            Pitch = 1f,
            PanStereo = 0f,
            Loop = false,
            StartTimeSeconds = 0f,
            EndTimeSeconds = 0f
        };

        public void Clamp()
        {
            Volume01 = Mathf.Clamp01(Volume01);
            Pitch = Mathf.Clamp(Pitch, 0.1f, 3f);
            PanStereo = Mathf.Clamp(PanStereo, -1f, 1f);
            StartTimeSeconds = Mathf.Max(0f, StartTimeSeconds);
        }
    }
}