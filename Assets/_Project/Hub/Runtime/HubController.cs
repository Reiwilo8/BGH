using System.Collections;
using Project.Core.App;
using Project.Core.Audio;
using Project.Core.Input;
using Project.Core.Localization;
using Project.Core.Settings;
using Project.Core.VisualAssist;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubController : MonoBehaviour
    {
        private HubStateMachine _sm;
        private IInputFocusService _focus;

        private ILocalizationService _loc;
        private IVisualAssistService _va;

        private bool _subscribed;
        private Coroutine _lateSubscribe;

        private void Awake()
        {
            var services = AppContext.Services;

            var uiAudio = services.Resolve<IUiAudioOrchestrator>();
            var flow = services.Resolve<IAppFlowService>();
            var settings = services.Resolve<ISettingsService>();

            _loc = services.Resolve<ILocalizationService>();
            _va = TryResolveVa(services);

            _sm = new HubStateMachine(uiAudio, flow, settings);
        }

        private void Start()
        {
            ApplyVisualAssistBaselineForHub();

            var session = AppContext.Services.Resolve<AppSession>();

            if (session.HubTarget == HubReturnTarget.GameSelect)
                _sm.SetState(new States.HubGameSelectState(_sm));
            else
                _sm.SetState(new States.HubMainState(_sm));
        }

        private void OnEnable()
        {
            TrySubscribe();

            if (!_subscribed && _lateSubscribe == null)
                _lateSubscribe = StartCoroutine(LateSubscribeRoutine());
        }

        private void OnDisable()
        {
            if (_lateSubscribe != null)
            {
                StopCoroutine(_lateSubscribe);
                _lateSubscribe = null;
            }

            TryUnsubscribe();
        }

        private IEnumerator LateSubscribeRoutine()
        {
            yield return null;
            yield return null;

            TrySubscribe();
            _lateSubscribe = null;
        }

        private void TrySubscribe()
        {
            if (_subscribed)
                return;

            try
            {
                if (_focus == null)
                    _focus = AppContext.Services.Resolve<IInputFocusService>();

                if (_focus == null)
                    return;

                _focus.OnChanged += OnFocusChanged;
                _subscribed = true;
            }
            catch
            {
            }
        }

        private void TryUnsubscribe()
        {
            if (!_subscribed)
                return;

            if (_focus != null)
                _focus.OnChanged -= OnFocusChanged;

            _subscribed = false;
        }

        private void OnFocusChanged(InputScope scope)
        {
            if (scope != InputScope.Hub)
                return;

            ApplyVisualAssistBaselineForHub();

            _sm.OnFocusGained();
        }

        public void OnRepeatRequested()
        {
            _sm.OnRepeatRequested();
        }

        public void Handle(NavAction action)
        {
            _sm.Dispatch(action);
        }

        private static IVisualAssistService TryResolveVa(Project.Core.Services.IServiceRegistry services)
        {
            try { return services.Resolve<IVisualAssistService>(); }
            catch { return null; }
        }

        private void ApplyVisualAssistBaselineForHub()
        {
            if (_va == null || _loc == null)
                return;

            _va.SetHeaderKey("va.app_name");
            _va.SetSubHeaderKey("va.screen.main_menu");

            _va.ClearTransitioning();
            _va.ClearDimmer();
            _va.ClearCenter(VaCenterLayer.Transition);
            _va.ClearCenter(VaCenterLayer.Gesture);

            _va.SetCenterText(VaCenterLayer.PlannedSpeech, _loc.Get("enter.main_menu"));
        }
    }
}