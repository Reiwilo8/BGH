namespace Project.Core.Speech
{
    public sealed class SpeechFeedRouter : ISpeechFeed
    {
        private ISpeechFeed _target;

        public void SetTarget(ISpeechFeed target) => _target = target;

        public void ClearTarget() => _target = null;

        public void ClearTarget(ISpeechFeed target)
        {
            if (_target == target) _target = null;
        }

        public void OnSpoken(string text, SpeechPriority priority)
        {
            _target?.OnSpoken(text, priority);
        }
    }
}
