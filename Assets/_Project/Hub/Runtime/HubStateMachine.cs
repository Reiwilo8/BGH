using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;

namespace Project.Hub
{
    public sealed class HubStateMachine
    {
        public readonly ISpeechService Speech;
        public readonly IAppFlowService Flow;

        private IHubState _current;

        public HubStateMachine(ISpeechService speech, IAppFlowService flow)
        {
            Speech = speech;
            Flow = flow;
        }

        public void SetState(IHubState next)
        {
            _current?.Exit();
            _current = next;
            _current?.Enter();
        }

        public void Dispatch(NavAction action) => _current?.Handle(action);
    }

    public interface IHubState
    {
        string Name { get; }
        void Enter();
        void Exit();
        void Handle(NavAction action);
    }
}