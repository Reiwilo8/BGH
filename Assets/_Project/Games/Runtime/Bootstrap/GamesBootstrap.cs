using Project.Core.App;
using Project.Games.Catalog;
using UnityEngine;

namespace Project.Games.Bootstrap
{
    public sealed class GamesBootstrap : MonoBehaviour
    {
        [SerializeField] private GameCatalog gameCatalog;

        private void Awake()
        {
            if (gameCatalog == null)
                throw new System.InvalidOperationException("GameCatalog is not assigned.");

            AppContext.Services.Register(gameCatalog);
        }
    }
}