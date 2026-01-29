using Project.Core.App;
using UnityEngine;

namespace Project.Games.Stats
{
    public sealed class GameRunStatsReporter : MonoBehaviour
    {
        private IGameRunContextService _runs;
        private IGameStatsService _stats;

        private bool _subscribed;

        private void OnEnable()
        {
            TryResolveServices();

            if (_runs == null || _stats == null)
                return;

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TryResolveServices()
        {
            if (_runs != null && _stats != null)
                return;

            var services = AppContext.Services;

            if (_runs == null)
            {
                try { _runs = services.Resolve<IGameRunContextService>(); }
                catch { _runs = null; }
            }

            if (_stats == null)
            {
                try { _stats = services.Resolve<IGameStatsService>(); }
                catch { _stats = null; }
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;

            _runs.RunStarted += OnRunStarted;
            _runs.RunFinished += OnRunFinished;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            if (_runs != null)
            {
                _runs.RunStarted -= OnRunStarted;
                _runs.RunFinished -= OnRunFinished;
            }

            _subscribed = false;
        }

        private void OnRunStarted(GameRunContext ctx)
        {
            if (_stats == null) return;
            if (string.IsNullOrWhiteSpace(ctx.GameId) || string.IsNullOrWhiteSpace(ctx.ModeId))
                return;

            _stats.RecordRunStarted(ctx.GameId, ctx.ModeId);
        }

        private void OnRunFinished(GameRunFinished ev)
        {
            if (_stats == null) return;

            var ctx = ev.Context;
            if (string.IsNullOrWhiteSpace(ctx.GameId) || string.IsNullOrWhiteSpace(ctx.ModeId))
                return;

            _stats.RecordRunFinished(
                gameId: ctx.GameId,
                modeId: ctx.ModeId,
                duration: ev.Duration,
                score: ev.Score,
                completed: ev.Completed
            );
        }
    }
}