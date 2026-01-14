using Project.Core.Localization;
using Project.Core.Speech;

namespace Project.Core.Audio
{
    public sealed class UiAudioContext
    {
        public ISpeechService Speech { get; }
        public ILocalizationService Localization { get; }
        public UiAudioSequenceHandle Handle { get; }

        internal UiAudioContext(
            ISpeechService speech,
            ILocalizationService localization,
            UiAudioSequenceHandle handle)
        {
            Speech = speech;
            Localization = localization;
            Handle = handle;
        }
    }
}