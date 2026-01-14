using System;

namespace Project.Core.Activity
{
    public interface IRepeatService
    {
        float IdleThresholdSeconds { get; set; }
        event Action RepeatRequested;
        void RequestRepeat(string source = null);
    }
}