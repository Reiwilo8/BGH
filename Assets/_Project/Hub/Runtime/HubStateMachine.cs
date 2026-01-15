using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Input;
using Project.Core.Settings;

namespace Project.Hub
{
    public sealed class HubStateMachine
    {
        public readonly IUiAudioOrchestrator UiAudio;
        public readonly IAppFlowService Flow;
        public readonly ISettingsService Settings;

        public readonly HubTransitionGate Transitions = new HubTransitionGate();

        private IHubState _current;

        public States.HubMainOption HubMainSelection { get; set; } = States.HubMainOption.GameSelect;

        public HubStateMachine(IUiAudioOrchestrator uiAudio, IAppFlowService flow, ISettingsService settings)
        {
            UiAudio = uiAudio;
            Flow = flow;
            Settings = settings;
        }

        public void SetState(IHubState next)
        {
            _current?.Exit();
            _current = next;
            _current?.Enter();
        }

        public void Dispatch(NavAction action) => _current?.Handle(action);
        public void OnFocusGained() => _current?.OnFocusGained();
        public void OnRepeatRequested() => _current?.OnRepeatRequested();
    }

    public interface IHubState
    {
        string Name { get; }
        void Enter();
        void Exit();
        void Handle(NavAction action);
        void OnFocusGained();
        void OnRepeatRequested();
    }
}