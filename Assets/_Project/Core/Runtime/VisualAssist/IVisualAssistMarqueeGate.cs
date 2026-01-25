namespace Project.Core.VisualAssist
{
    public interface IVisualAssistMarqueeGate
    {
        bool IsWaitingForFirstMarqueePass { get; }

        void BeginWaitForFirstMarqueePass();
        void CompleteWaitForFirstMarqueePass();
        void ForceRelease();
    }
}