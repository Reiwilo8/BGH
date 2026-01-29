using Project.Core.App;
using Project.Games.Run;
using UnityEngine;

namespace Project.Games.Run
{
    public sealed class GameRunSeedHistoryReporter : MonoBehaviour
    {
        private IGameRunContextService _runs;
        private IGameRunParametersService _params;

        private bool _subscribed;

        private void OnEnable()
        {
            TryResolveServices();

            if (_runs == null || _params == null)
                return;

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TryResolveServices()
        {
            var services = AppContext.Services;

            if (_runs == null)
            {
                try { _runs = services.Resolve<IGameRunContextService>(); }
                catch { _runs = null; }
            }

            if (_params == null)
            {
                try { _params = services.Resolve<IGameRunParametersService>(); }
                catch { _params = null; }
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;

            _runs.RunPrepared += OnRunPrepared;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (_runs != null)
                _runs.RunPrepared -= OnRunPrepared;

            _subscribed = false;
        }

        private void OnRunPrepared(GameRunContext ctx)
        {
            if (_params == null) return;
            if (string.IsNullOrWhiteSpace(ctx.GameId)) return;
            if (!ctx.Seed.HasValue) return;

            _params.AddKnownSeed(ctx.GameId, ctx.Seed.Value);
        }
    }
}