namespace Project.Core.Activity
{
    public interface IUserInactivityService
    {
        float SecondsSinceLastNavAction { get; }
        void MarkNavAction();
        bool IsIdle(float thresholdSeconds);
    }
}