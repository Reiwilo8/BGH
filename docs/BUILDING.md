# Building & Installation

This project is developed using **Unity 6.0 LTS (6000.0.64f1)**.

---

## Unity Version

Required:
- **Unity 6.0 LTS (6000.0.64f1)**

Newer LTS versions (e.g. 6.3 LTS) are not officially targeted.

---

## Android

- Minimum Android version: **Android 6.0 (API 23)**
- Uses system-level `TextToSpeech`
- Offline TTS supported depending on installed system voices

Build notes:
- Target architecture: ARMv7 / ARM64
- Orientation: landscape only
- Touch input required for full gesture support

---

## iOS

- Uses native `AVSpeechSynthesizer`
- TTS implemented via Objective-C++ plugin (`.mm`)
- Requires device for full testing

---

## Windows (Standalone)

- Uses Microsoft SAPI via native C++ DLL
- Architecture: x86_64
- Single-instance enforced at application level

---

## macOS / Linux (Experimental)

- Application can be built and run
- No native TTS implementation provided
- Audio feedback limited to non-speech cues

---

## Editor / Simulation Mode

- Unity Editor supports simulated input
- Mouse and keyboard can emulate gestures
- TTS is simulated via log output (`EditorSpeechService`)