using Project.Core.Input;
using Project.Core.Services;
using Project.Core.Speech;
using Project.Core.Visual;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Project.Core.App
{
    [DefaultExecutionOrder(-10000)]
    public sealed class AppRootBootstrap : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private string startSceneName = "StartScene";
        [SerializeField] private string hubSceneName = "HubScene";

        private IServiceRegistry _services;
        public IServiceRegistry Services => _services;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            _services = new ServiceRegistry();

            _services.Register(new AppSession());

            _services.Register<IVisualModeService>(new VisualModeService());

            var feedRouter = new SpeechFeedRouter();
            _services.Register(feedRouter);

            var speech = SpeechServiceFactory.Create(feedRouter);
            _services.Register<ISpeechService>(speech);

            var appFlow = new AppFlowService(startScene: startSceneName, hubScene: hubSceneName);
            _services.Register<IAppFlowService>(appFlow);

            _services.Register<IInputService>(new InputService());
            _services.Register<IInputFocusService>(new InputFocusService());
        }

        private void Start()
        {
            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            try
            {
                var flow = AppContext.Services.Resolve<IAppFlowService>();
                await flow.EnterStartAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}