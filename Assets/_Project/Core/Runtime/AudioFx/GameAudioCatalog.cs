using System;
using UnityEngine;

namespace Project.Core.AudioFx
{
    [CreateAssetMenu(menuName = "Project/AudioFx/Game Audio Catalog")]
    public sealed class GameAudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string soundId;
            public AudioClip clip;
            [Range(0f, 1f)] public float defaultVolume01 = 1f;
        }

        [Header("Identity")]
        public string gameId;

        [Header("Sounds")]
        public Entry[] sounds;

        public bool TryGet(string soundId, out AudioClip clip, out float defaultVolume01)
        {
            if (string.IsNullOrEmpty(soundId))
            {
                clip = null; defaultVolume01 = 1f; return false;
            }

            if (sounds != null)
            {
                for (int i = 0; i < sounds.Length; i++)
                {
                    var e = sounds[i];
                    if (e == null) continue;
                    if (!string.Equals(e.soundId, soundId, StringComparison.Ordinal)) continue;

                    clip = e.clip;
                    defaultVolume01 = Mathf.Clamp01(e.defaultVolume01);
                    return clip != null;
                }
            }

            clip = null;
            defaultVolume01 = 1f;
            return false;
        }
    }
}