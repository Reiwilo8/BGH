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

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
                controller.Next();

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
                controller.Previous();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                controller.Confirm();

            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
                controller.Back();

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
                controller.Confirm();

            if (rmb)
                controller.Back();
        }
    }
}