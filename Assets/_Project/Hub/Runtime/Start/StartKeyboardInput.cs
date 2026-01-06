using UnityEngine;

namespace Project.Hub.Start
{
    public sealed class StartKeyboardInput : MonoBehaviour
    {
        [SerializeField] private StartController controller;

        private bool _ignoreNextMouseClick;

        private void Reset()
        {
            controller = FindFirstObjectByType<StartController>();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                _ignoreNextMouseClick = true;
            }
        }

        private void Update()
        {
            if (controller == null) return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                controller.EnterHub();

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
                controller.ExitApp();

            if (Input.GetKeyDown(KeyCode.F1))
                controller.ToggleVisualAssist();

            bool lmb = Input.GetMouseButtonDown(0);
            bool rmb = Input.GetMouseButtonDown(1);

            if (_ignoreNextMouseClick && (lmb || rmb))
            {
                _ignoreNextMouseClick = false;
                return;
            }

            if (lmb)
                controller.EnterHub();

            if (rmb)
                controller.ExitApp();
        }
    }
}