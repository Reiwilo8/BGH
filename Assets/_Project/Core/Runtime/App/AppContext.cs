using Project.Core.Services;
using UnityEngine;

namespace Project.Core.App
{
    public static class AppContext
    {
        private static IServiceRegistry _services;

        public static IServiceRegistry Services
        {
            get
            {
                if (_services != null) return _services;

                var bootstrap = Object.FindFirstObjectByType<AppRootBootstrap>();
                if (bootstrap == null)
                    throw new System.InvalidOperationException("AppRootBootstrap not found. Ensure AppRoot scene is loaded.");

                _services = bootstrap.Services;
                if (_services == null)
                    throw new System.InvalidOperationException("AppRootBootstrap.Services is not initialized yet. Check script execution order.");
                return _services;
            }
        }

        public static void ResetCacheForDomainReload()
        {
            _services = null;
        }
    }
}