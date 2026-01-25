using System;

namespace Project.Core.VisualAssist
{
    public enum VaCenterLayer
    {
        None = 0,
        IdleHint = 10,
        PlannedSpeech = 20,
        Transition = 30,
        Gesture = 40,
    }

    public enum VaListMoveDirection
    {
        None = 0,
        Next = 1,
        Previous = 2
    }

    public interface IVisualAssistService
    {
        event Action Changed;

        string Header { get; }
        string SubHeader { get; }
        string CenterText { get; }
        float DimAlpha01 { get; }

        bool IsTransitioning { get; }

        int ListMovePulse { get; }
        VaListMoveDirection LastListMoveDirection { get; }

        void SetHeaderKey(string key, params object[] args);
        void SetHeaderText(string text);

        void SetSubHeaderKey(string key, params object[] args);
        void SetSubHeaderText(string text);

        void SetCenterKey(VaCenterLayer layer, string key, params object[] args);
        void SetCenterText(VaCenterLayer layer, string text);
        void ClearCenter(VaCenterLayer layer);

        void SetIdleHintKey(string key, params object[] args);
        void SetIdleHintText(string text);
        void ClearIdleHint();

        void NotifyPlannedSpeech(string fullText);

        void NotifyTransitioning();
        void ClearTransitioning();

        void PulseListMove(VaListMoveDirection direction);

        void SetDimAlpha01(float alpha01);
        void ClearDimmer();

        void FlashRepeat(float seconds = 0.25f);

        void EvaluateIdleHint(bool canShow, float idleSeconds);
    }
}