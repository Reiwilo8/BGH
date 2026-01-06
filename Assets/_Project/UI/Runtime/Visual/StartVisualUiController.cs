using Project.Core.Speech;
using Project.Core.Visual;
using TMPro;
using UnityEngine;

namespace Project.UI.Visual
{
    public sealed class StartVisualUiController : MonoBehaviour, ISpeechFeed
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text mainText;

        private string _baseContent = "";
        private string _lastSpoken = "";

        public void ApplyMode(VisualMode mode)
        {
            if (root != null)
                root.SetActive(mode == VisualMode.VisualAssist);
        }

        public void SetContent(string text)
        {
            _baseContent = text ?? "";
            Render();
        }

        public void OnSpoken(string text, SpeechPriority priority)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _lastSpoken = $"Last spoken [{priority}]: {text}";
            Render();
        }

        private void Render()
        {
            if (mainText == null) return;

            if (string.IsNullOrWhiteSpace(_lastSpoken))
                mainText.text = _baseContent;
            else
                mainText.text = _baseContent + "\n\n" + _lastSpoken;
        }
    }
}