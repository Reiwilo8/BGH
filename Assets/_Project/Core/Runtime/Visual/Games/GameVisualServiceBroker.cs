using UnityEngine;

namespace Project.Core.Visual.Games
{
    public sealed class GameVisualServiceBroker : IGameVisualService
    {
        private IGameVisualView _view;
        private bool _visible;

        public bool HasActiveView => _view != null;

        public void RegisterView(IGameVisualView view)
        {
            if (view == null)
                return;

            if (ReferenceEquals(_view, view))
            {
                _view.SetVisible(_visible);
                return;
            }

            _view = view;
            _view.SetVisible(_visible);
        }

        public void UnregisterView(IGameVisualView view)
        {
            if (view == null)
                return;

            if (!ReferenceEquals(_view, view))
                return;

            _view = null;
        }

        public void Reset()
        {
            _view?.Reset();
        }

        public void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;
            _view?.SetVisible(_visible);
        }

        public void SubmitState(GameVisualState state)
        {
            if (_view == null || !_visible)
                return;

            if (state == null)
                return;

            _view.ApplyState(state);
        }

        public void Emit(GameVisualEvent e)
        {
            if (_view == null || !_visible)
                return;

            if (string.IsNullOrWhiteSpace(e.Name))
                return;

            _view.HandleEvent(e);
        }
    }
}