using System.Collections.Generic;

namespace Project.Games.Run
{
    public interface IGameInitialParametersProvider
    {
        void AppendParameters(string gameId, string modeId, IDictionary<string, string> initialParameters);
    }
}