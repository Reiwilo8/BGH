using System;
using UnityEngine;

namespace Project.Core.AudioFx
{
    [CreateAssetMenu(menuName = "Project/AudioFx/Common Game Sounds Catalog")]
    public sealed class CommonGameSoundsCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public CommonGameSoundId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float defaultVolume01 = 1f;
        }

        public Entry[] entries;

        public bool TryGet(CommonGameSoundId id, out AudioClip clip, out float defaultVolume01)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e == null) continue;
                    if (e.id != id) continue;

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