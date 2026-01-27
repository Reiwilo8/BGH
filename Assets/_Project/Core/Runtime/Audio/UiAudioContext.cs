using Project.Core.AudioFx;
using Project.Core.Localization;
using Project.Core.Speech;
using Project.Core.VisualAssist;

namespace Project.Core.Audio
{
    public sealed class UiAudioContext
    {
        public ISpeechService Speech { get; }
        public ILocalizationService Localization { get; }
        public IVisualAssistService VisualAssist { get; }
        public IAudioFxService AudioFx { get; }
        public UiAudioSequenceHandle Handle { get; }

        internal UiAudioContext(
            ISpeechService speech,
            ILocalizationService localization,
            IVisualAssistService visualAssist,
            IAudioFxService audioFx,
            UiAudioSequenceHandle handle)
        {
            Speech = speech;
            Localization = localization;
            VisualAssist = visualAssist;
            AudioFx = audioFx;
            Handle = handle;
        }
    }
}