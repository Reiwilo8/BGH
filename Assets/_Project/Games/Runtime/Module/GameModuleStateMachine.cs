using Project.Core.App;
using Project.Core.Audio;
using Project.Core.AudioFx;
using Project.Core.Input;
using Project.Core.Settings;
using Project.Games.Module.States;

namespace Project.Games.Module
{
    public sealed class GameModuleStateMachine
    {
        public readonly IUiAudioOrchestrator UiAudio;
        public readonly IAppFlowService Flow;
        public readonly ISettingsService Settings;
        public readonly IAudioFxService AudioFx;

        public readonly GameModuleTransitionGate Transitions = new GameModuleTransitionGate();

        private IGameModuleState _current;

        public GameModuleStateMachine(
            IUiAudioOrchestrator uiAudio,
            IAppFlowService flow,
            ISettingsService settings)
        {
            UiAudio = uiAudio;
            Flow = flow;
            Settings = settings;

            AudioFx = AppContext.Services.Resolve<IAudioFxService>();
        }

        public void SetState(IGameModuleState next)
        {
            _current?.Exit();
            _current = next;
            _current?.Enter();
        }

        public void Dispatch(NavAction action)
        {
            if (action == NavAction.ToggleVisualAssist)
                return;

            if (_current is GameMenuState menu && action == NavAction.Confirm)
            {
                AudioFx?.PlayUiCue(menu.IsConfirmingBackItem() ? UiCueId.Back : UiCueId.Confirm);
                _current.Handle(action);
                return;
            }

            PlayNavCue(action);
            _current?.Handle(action);
        }

        public void OnFocusGained() => _current?.OnFocusGained();

        public void OnRepeatRequested()
        {
            AudioFx?.PlayUiCue(UiCueId.Repeat);
            _current?.OnRepeatRequested();
        }

        private void PlayNavCue(NavAction action)
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

    public interface IGameModuleState
    {
        string Name { get; }
        void Enter();
        void Exit();
        void Handle(NavAction action);
        void OnFocusGained();
        void OnRepeatRequested();
    }
}