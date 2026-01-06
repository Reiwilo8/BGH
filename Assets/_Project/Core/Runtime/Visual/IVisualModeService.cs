namespace Project.Core.Visual
{
    public interface IVisualModeService
    {
        VisualMode Mode { get; }
        void SetMode(VisualMode mode);
        void ToggleVisualAssist();
    }
}