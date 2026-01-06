using Project.Core.Input;
using UnityEngine;

namespace Project.Hub
{
    public sealed class HubKeyboardInput : MonoBehaviour
    {
        [SerializeField] private HubController hub;

        private void Reset()
        {
            hub = FindFirstObjectByType<HubController>();
        }

        private void Update()
        {
            if (hub == null) return;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
                hub.Handle(NavAction.Next);

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
                hub.Handle(NavAction.Previous);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                hub.Handle(NavAction.Confirm);

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                hub.Handle(NavAction.Back);
        }
    }
}