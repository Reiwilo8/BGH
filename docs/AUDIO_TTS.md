# Audio + TTS

## Concepts

### Audio FX (non-speech)
- `AudioFxService` owns UI cues and “game sounds” buses.
- Boot registers:
  - `UiCuesCatalog`
  - `CommonGameSoundsCatalog`
  - `GameAudioCatalogRegistry`
- Volume & enable flags are driven from `ISettingsService`.

### Speech (TTS)
`ISpeechService` has a platform factory:
- Android: `AndroidSpeechService` (bridge to native)
- iOS: `IosSpeechService` (bridge to native)
- Windows: `WindowsSpeechService` (native DLL)
- Editor: `EditorSpeechService` (logs only)

Native plugin locations:
- `Assets/Plugins/Android` → `TtsBridgeAndroid` (and receiver GO)
- `Assets/Plugins/iOS` → `TtsBridgeIos`
- `Assets/Plugins/x86_64` → `TtsBridgeWin` (Windows)

> macOS/Linux builds run, but without TTS (unless a new bridge is added).

---

## UiAudioOrchestrator

### Why it exists
- A single place that speaks *UI narration* from localization keys.
- Provides:
  - prioritization (`SpeechPriority`)
  - interruptibility (some sequences are “must finish”)
  - pending-queue behavior when current is non-interruptible

### Public API
- `Play(scope, sequence, priority, interruptible)`
- `CancelCurrent()`
- `PlayGated(...)` (useful for “still transitioning” voice messages)

### Typical step usage
`UiAudioSteps.SpeakKeyAndWait(ctx, "some.key")`
- gets localized string
- notifies Visual Assist with planned speech text
- speaks and waits for completion
- optionally gates on first marquee pass via `IVisualAssistMarqueeGate`

---

## Speech priority (concept)

- Low / Normal / High / Interrupt
- Orchestrator won’t interrupt current speech if:
  - current is non-interruptible, OR
  - new request is lower priority than current
