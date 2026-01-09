using System.Threading.Tasks;
using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;

namespace Project.Hub.States
{
    public enum HubMainOption
    {
        GameSelect = 0,
        Settings = 1,
        Exit = 2
    }

    public sealed class HubMainState : IHubState
    {
        private readonly HubStateMachine _sm;
        private HubMainOption _current = HubMainOption.GameSelect;

        public string Name => "Hub.Main";

        public HubMainState(HubStateMachine sm) => _sm = sm;

        public void Enter()
        {
            AppContext.Services.Resolve<AppSession>().SetHubTarget(HubReturnTarget.Main);
            AnnounceCurrent(includeHelp: true);
        }

        public void Exit() { }

        public void OnFocusGained()
        {
            AnnounceCurrent(includeHelp: true);
        }

        public void Handle(NavAction action)
        {
            switch (action)
            {
                case NavAction.Next:
                    _current = (HubMainOption)(((int)_current + 1) % 3);
                    AnnounceCurrent(includeHelp: false);
                    break;

                case NavAction.Previous:
                    _current = (HubMainOption)(((int)_current - 1 + 3) % 3);
                    AnnounceCurrent(includeHelp: false);
                    break;

                case NavAction.Confirm:
                    _ = ConfirmAsync();
                    break;

                case NavAction.Back:
                    _ = ReturnToStartAsync();
                    break;
            }
        }

        private void AnnounceCurrent(bool includeHelp)
        {
            string optionText = _current switch
            {
                HubMainOption.GameSelect => "Game Select.",
                HubMainOption.Settings => "Settings.",
                HubMainOption.Exit => "Exit application.",
                _ => "Unknown option."
            };

            string help = includeHelp
                ? " Use Next or Previous to change option. Confirm to select. Back to return to the Start screen."
                : " Confirm to select.";

            _sm.Speech.Speak($"Hub. {optionText}{help}", SpeechPriority.Normal);
        }

        private async Task ConfirmAsync()
        {
            if (_sm.Flow.IsTransitioning)
                return;

            switch (_current)
            {
                case HubMainOption.GameSelect:
                    _sm.SetState(new HubGameSelectState(_sm));
                    break;

                case HubMainOption.Settings:
                    _sm.Speech.Speak("Settings are not implemented yet.", SpeechPriority.High);
                    break;

                case HubMainOption.Exit:
                    _sm.Speech.Speak("Quitting application.", SpeechPriority.High);
                    await _sm.Flow.ExitApplicationAsync();
                    break;
            }
        }

        private async Task ReturnToStartAsync()
        {
            if (_sm.Flow.IsTransitioning)
                return;

            _sm.Speech.Speak("Returning to the Start screen.", SpeechPriority.High);
            await _sm.Flow.EnterStartAsync();
        }
    }
}