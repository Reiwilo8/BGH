using UnityEngine;

namespace Project.Core.AudioFx
{
    public sealed class AudioFxHandle
    {
        internal AudioSource Source;
        internal bool StopRequested;

        public bool IsValid => Source != null;
        public bool IsPlaying => IsValid && Source.isPlaying;
        public bool IsPaused => IsValid && !Source.isPlaying && Source.time > 0f && !StopRequested;

        public void Pause()
        {
            if (!IsValid) return;
            Source.Pause();
        }

        public void Resume()
        {
            if (!IsValid) return;
            Source.UnPause();
        }

        public void Stop()
        {
            if (!IsValid) return;
            StopRequested = true;
            Source.Stop();
        }

        public void SetPitch(float pitch)
        {
            if (!IsValid) return;
            Source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        }

        public void SetPan(float panStereo)
        {
            if (!IsValid) return;
            Source.panStereo = Mathf.Clamp(panStereo, -1f, 1f);
        }

        public void SetVolume01(float volume01)
        {
            if (!IsValid) return;
            Source.volume = Mathf.Clamp01(volume01);
        }

        public void Seek(float timeSeconds)
        {
            if (!IsValid) return;
            if (Source.clip == null) return;

            timeSeconds = Mathf.Clamp(timeSeconds, 0f, Source.clip.length);
            Source.time = timeSeconds;
        }
    }
}