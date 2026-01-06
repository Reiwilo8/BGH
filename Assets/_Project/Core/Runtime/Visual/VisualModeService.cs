using UnityEngine;

namespace Project.Core.Visual
{
    public sealed class VisualModeService : IVisualModeService
    {
        public VisualMode Mode { get; private set; } = VisualMode.AudioOnly;

        public void SetMode(VisualMode mode)
        {
            Mode = mode;
            Debug.Log($"[VisualMode] Mode = {Mode}");
        }

        public void ToggleVisualAssist()
        {
            SetMode(Mode == VisualMode.VisualAssist ? VisualMode.AudioOnly : VisualMode.VisualAssist);
        }
    }
}