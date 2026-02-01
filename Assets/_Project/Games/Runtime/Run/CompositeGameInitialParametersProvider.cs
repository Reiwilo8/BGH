using System;
using System.Collections.Generic;

namespace Project.Games.Run
{
    public sealed class CompositeGameInitialParametersProvider : IGameInitialParametersProvider
    {
        private readonly IGameInitialParametersProvider[] _providers;

        public CompositeGameInitialParametersProvider(params IGameInitialParametersProvider[] providers)
        {
            _providers = providers ?? Array.Empty<IGameInitialParametersProvider>();
        }

        public void AppendParameters(string gameId, string modeId, IDictionary<string, string> initialParameters)
        {
            if (initialParameters == null) return;
            if (string.IsNullOrWhiteSpace(gameId)) return;
            if (_providers == null || _providers.Length == 0) return;

            for (int i = 0; i < _providers.Length; i++)
            {
                var p = _providers[i];
                if (p == null) continue;

                try
                {
                    p.AppendParameters(gameId, modeId, initialParameters);
                }
                catch
                {
                }
            }
        }
    }
}