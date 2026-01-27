using System;
using UnityEngine;

namespace Project.Core.AudioFx
{
    [CreateAssetMenu(menuName = "Project/AudioFx/AudioFx Catalog")]
    public sealed class AudioFxCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class UiCueEntry
        {
            public UiCueId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float defaultVolume01 = 1f;
        }

        public UiCueEntry[] uiCues;

        public bool TryGet(UiCueId id, out AudioClip clip, out float defaultVolume01)
        {
            if (uiCues != null)
            {
                for (int i = 0; i < uiCues.Length; i++)
                {
                    var e = uiCues[i];
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