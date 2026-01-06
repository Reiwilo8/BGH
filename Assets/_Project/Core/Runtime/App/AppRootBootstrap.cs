using Project.Core.Services;
using Project.Core.Speech;
using Project.Core.Visual;
using UnityEngine;

namespace Project.Core.App
{
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

            _services.Register<IVisualModeService>(new VisualModeService());

            var feedRouter = new SpeechFeedRouter();
            _services.Register(feedRouter);

            var speech = SpeechServiceFactory.Create(feedRouter);
            _services.Register<ISpeechService>(speech);

            var appFlow = new AppFlowService(startScene: startSceneName, hubScene: hubSceneName);
            _services.Register<IAppFlowService>(appFlow);
        }

        private async void Start()
        {
            try
            {
                var flow = _services.Resolve<IAppFlowService>();
                await flow.EnterStartAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}