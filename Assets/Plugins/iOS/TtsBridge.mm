#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer* gSynth = nil;
static bool gIsSpeaking = false;

@interface TtsDelegate : NSObject<AVSpeechSynthesizerDelegate>
@end

@implementation TtsDelegate
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synth didStartSpeechUtterance:(AVSpeechUtterance *)utterance {
    gIsSpeaking = true;
}
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synth didFinishSpeechUtterance:(AVSpeechUtterance *)utterance {
    gIsSpeaking = false;
}
- (void)speechSynthesizer:(AVSpeechSynthesizer *)synth didCancelSpeechUtterance:(AVSpeechUtterance *)utterance {
    gIsSpeaking = false;
}
@end

static TtsDelegate* gDelegate = nil;

static NSString* MakeNSString(const char* cstr)
{
    if (cstr == NULL) return @"";
    return [NSString stringWithUTF8String:cstr] ?: @"";
}

extern "C" {

void TTS_Init()
{
    if (gSynth == nil)
    {
        gSynth = [[AVSpeechSynthesizer alloc] init];
        gDelegate = [[TtsDelegate alloc] init];
        gSynth.delegate = gDelegate;
        gIsSpeaking = false;
    }
}

bool TTS_IsSpeaking()
{
    if (gSynth == nil) return false;
    return gIsSpeaking || gSynth.isSpeaking;
}

void TTS_Stop()
{
    if (gSynth == nil) return;

    [gSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    gIsSpeaking = false;
}

void TTS_Speak(const char* text, const char* lang)
{
    if (gSynth == nil) TTS_Init();
    if (gSynth == nil) return;

    NSString* nsText = MakeNSString(text);
    if (nsText.length == 0) return;

    [gSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    gIsSpeaking = false;

    AVSpeechUtterance* utt = [[AVSpeechUtterance alloc] initWithString:nsText];

    NSString* langCode = MakeNSString(lang);
    if (langCode.length == 0) langCode = @"en-US";

    AVSpeechSynthesisVoice* voice = [AVSpeechSynthesisVoice voiceWithLanguage:langCode];
    if (voice != nil)
        utt.voice = voice;

    [gSynth speakUtterance:utt];
}

} // extern "C"