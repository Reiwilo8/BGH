using UnityEngine;
using Project.Games.Definitions;

namespace Project.Games.Catalog
{
    [CreateAssetMenu(menuName = "Project/Games/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public GameDefinition[] games;

        public GameDefinition GetById(string gameId)
        {
            if (games == null) return null;
            foreach (var g in games)
                if (g != null && g.gameId == gameId) return g;
            return null;
        }
    }
}