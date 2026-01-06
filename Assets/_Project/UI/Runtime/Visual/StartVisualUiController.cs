using Project.Core.Visual;
using TMPro;
using UnityEngine;

namespace Project.UI.Visual
{
    public sealed class StartVisualUiController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text mainText;

        public void ApplyMode(VisualMode mode)
        {
            if (root != null)
                root.SetActive(mode == VisualMode.VisualAssist);
        }

        public void SetContent(string text)
        {
            if (mainText != null)
                mainText.text = text;
        }
    }
}