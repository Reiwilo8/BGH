using Project.Core.App;
using Project.Core.Audio;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Settings;

namespace Project.Hub
{
    public sealed class HubStateMachine
    {
        public readonly IUiAudioOrchestrator UiAudio;
        public readonly IAppFlowService Flow;
        public readonly ISettingsService Settings;
        public readonly IAudioFxService AudioFx;

        public readonly HubTransitionGate Transitions = new HubTransitionGate();

        private IHubState _current;

        public States.HubMainOption HubMainSelection { get; set; } = States.HubMainOption.GameSelect;

        public HubStateMachine(IUiAudioOrchestrator uiAudio, IAppFlowService flow, ISettingsService settings)
        {
            UiAudio = uiAudio;
            Flow = flow;
            Settings = settings;

            AudioFx = AppContext.Services.Resolve<IAudioFxService>();
        }

        public void SetState(IHubState next)
        {
            _current?.Exit();
            _current = next;
            _current?.Enter();
        }

        public void Dispatch(NavAction action)
        {
            if (_current is States.HubSettingsState)
            {
                _current.Handle(action);
                return;
            }

            if (_current is States.HubGameSelectState gs && action == NavAction.Confirm)
            {
                if (gs.IsConfirmingBackItem())
                    AudioFx?.PlayUiCue(UiCueId.Back);
                else
                    AudioFx?.PlayUiCue(UiCueId.Confirm);

                _current.Handle(action);
                return;
            }

            PlayNavCueForHub(action);
            _current?.Handle(action);
        }

        public void OnFocusGained() => _current?.OnFocusGained();

        public void OnRepeatRequested()
        {
            AudioFx?.PlayUiCue(UiCueId.Repeat);
            _current?.OnRepeatRequested();
        }

        private void PlayNavCueForHub(NavAction action)
        {
            switch (action)
            {
                case NavAction.Next:
                    AudioFx?.PlayUiCue(UiCueId.NavigateNext);
                    break;

                case NavAction.Previous:
                    AudioFx?.PlayUiCue(UiCueId.NavigatePrevious);
                    break;

                case NavAction.Confirm:
                    AudioFx?.PlayUiCue(UiCueId.Confirm);
                    break;

                case NavAction.Back:
                    AudioFx?.PlayUiCue(UiCueId.Back);
                    break;
            }
        }
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