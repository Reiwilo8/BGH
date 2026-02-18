using Project.Core.App;
using UnityEngine;

namespace Project.Core.Visual.Games
{
    public sealed class GameVisualHost : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private MonoBehaviour viewBehaviour;

        private IVisualModeService _visualMode;
        private IGameVisualService _gameVisual;
        private IGameVisualView _view;

        private bool _lastVisible;

        private void Awake()
        {
            var services = AppContext.Services;

            _visualMode = services.Resolve<IVisualModeService>();
            _gameVisual = services.Resolve<IGameVisualService>();

            _view = viewBehaviour as IGameVisualView;

            if (_view == null && viewBehaviour != null)
                Debug.LogError($"[GameVisualHost] Assigned viewBehaviour does not implement IGameVisualView: {viewBehaviour.GetType().Name}");
        }

        private void OnEnable()
        {
            if (_gameVisual != null && _view != null)
                _gameVisual.RegisterView(_view);

            ApplyVisibility(force: true);
        }

        private void OnDisable()
        {
            if (_gameVisual != null && _view != null)
                _gameVisual.UnregisterView(_view);

            if (root != null)
                root.SetActive(false);

            _lastVisible = false;
        }

        private void Update()
        {
            ApplyVisibility(force: false);
        }

        private void ApplyVisibility(bool force)
        {
            bool visible = _visualMode != null && _visualMode.Mode == VisualMode.VisualAssist;

            if (!force && visible == _lastVisible)
                return;

            _lastVisible = visible;

            if (_gameVisual != null)
                _gameVisual.SetVisible(visible);

            if (root != null)
                root.SetActive(visible);

            if (!visible)
                _view?.Reset();
        }
    }
}