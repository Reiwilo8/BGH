using System.Collections;

namespace Project.Core.Audio.Sequences
{
    public interface IUiAudioSequence
    {
        bool IsRepeatable { get; }
        IEnumerator Run(UiAudioContext ctx, UiAudioSequenceHandle handle);
    }
}