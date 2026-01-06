using Project.Core.App;
using Project.Core.Input;
using Project.Core.Speech;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubController : MonoBehaviour
    {
        private HubStateMachine _sm;

        private void Awake()
        {
            var speech = App.Services.Resolve<ISpeechService>();
            var flow = App.Services.Resolve<IAppFlowService>();

            _sm = new HubStateMachine(speech, flow);
        }

        private void Start()
        {
            _sm.SetState(new States.HubMainState(_sm));
        }

        public void Handle(NavAction action) => _sm.Dispatch(action);
    }
}