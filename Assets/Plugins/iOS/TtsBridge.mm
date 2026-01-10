#import <AVFoundation/AVFoundation.h>

static AVSpeechSynthesizer* gSynth = nil;

extern "C" {

    void TTS_Init()
    {
        if (gSynth == nil)
        {
            gSynth = [[AVSpeechSynthesizer alloc] init];
        }
    }

    void TTS_Speak(const char* text, const char* lang)
    {
        TTS_Init();
        if (text == NULL) return;

        // Interrupt/flush current speech to get "latest wins" behavior
        if (gSynth != nil && [gSynth isSpeaking])
        {
            [gSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
        }

        NSString* s = [NSString stringWithUTF8String:text];
        AVSpeechUtterance* utter = [AVSpeechUtterance speechUtteranceWithString:s];

        if (lang != NULL)
        {
            NSString* l = [NSString stringWithUTF8String:lang];
            AVSpeechSynthesisVoice* voice = [AVSpeechSynthesisVoice voiceWithLanguage:l];
            if (voice != nil)
                utter.voice = voice;
        }

        [gSynth speakUtterance:utter];
    }

    void TTS_Stop()
    {
        if (gSynth != nil)
        {
            [gSynth stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
        }
    }
}