using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.AudioFx
{
    [CreateAssetMenu(menuName = "Project/AudioFx/Game Audio Catalog Registry")]
    public sealed class GameAudioCatalogRegistry : ScriptableObject
    {
        public GameAudioCatalog[] catalogs;

        private Dictionary<string, GameAudioCatalog> _map;

        private void OnEnable()
        {
            _map = new Dictionary<string, GameAudioCatalog>();
            if (catalogs == null) return;

            for (int i = 0; i < catalogs.Length; i++)
            {
                var c = catalogs[i];
                if (c == null) continue;
                if (string.IsNullOrEmpty(c.gameId)) continue;

                _map[c.gameId] = c;
            }
        }

        public bool TryGet(string gameId, out GameAudioCatalog catalog)
        {
            catalog = null;
            if (_map == null) OnEnable();
            if (string.IsNullOrEmpty(gameId)) return false;
            return _map.TryGetValue(gameId, out catalog) && catalog != null;
        }
    }
}