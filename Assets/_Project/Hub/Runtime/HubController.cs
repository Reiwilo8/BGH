using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubController : MonoBehaviour
    {
        private HubStateMachine _sm;
        private IInputFocusService _focus;

        private void Awake()
        {
            var speech = AppContext.Services.Resolve<ISpeechService>();
            var flow = AppContext.Services.Resolve<IAppFlowService>();

            _sm = new HubStateMachine(speech, flow);
            _focus = AppContext.Services.Resolve<IInputFocusService>();
        }

        private void Start()
        {
            var session = AppContext.Services.Resolve<AppSession>();

            if (session.HubTarget == HubReturnTarget.GameSelect)
                _sm.SetState(new States.HubGameSelectState(_sm));
            else
                _sm.SetState(new States.HubMainState(_sm));
        }

        private void OnEnable() => _focus.OnChanged += OnFocusChanged;

        private void OnDisable() => _focus.OnChanged -= OnFocusChanged;

        private void OnFocusChanged(InputScope scope)
        {
            if (scope != InputScope.Hub) return;

            _sm.OnFocusGained();
        }

        public void Handle(NavAction action) => _sm.Dispatch(action);
    }
}