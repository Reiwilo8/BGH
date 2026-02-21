# Building (Unity 6.0 LTS)

This project targets Unity **6.0 LTS** (`6000.0.64f1`).

## Unity 6.0 player minimums (from Unity docs)

Mobile:
- Android: **6.0 (API 23)+**, OpenGL ES 3.0+ / Vulkan, ARMv7 (Neon) or ARM64, 1GB+ RAM
- iOS/iPadOS: **13+**, A8 SoC+, Metal, Xcode 15+

Editor (for reference):
- Windows: Windows 10 21H1+ (x64) / Windows 11 21H2+ (Arm64)
- macOS: Big Sur 11+
- Linux: Ubuntu 22.04 / 24.04

---

## Common preparation

1. Unity Hub → install Unity **6.0 LTS** + platform modules.
2. Open project.
3. Ensure scenes are in Build Settings in correct order (see `SCENES.txt`).
4. Confirm orientation:
   - Mobile: landscape only (left/right).

---

## Android build

1. Build Settings → Android → Switch Platform
2. Player Settings:
   - Package name, version code/name
   - Orientation: Landscape only
3. Build (APK) or Build App Bundle (AAB).

**TTS note**
- Android TTS is via plugin/bridge in `Assets/Plugins/Android`.

---

## iOS build (macOS)

1. Build Settings → iOS → Switch Platform
2. Build → outputs Xcode project
3. Open in Xcode, configure signing, run/archive.

**TTS note**
- iOS TTS is via plugin/bridge in `Assets/Plugins/iOS`.

---

## Windows build

1. Build Settings → Windows → Switch Platform
2. Build → standalone

**TTS note**
- Windows TTS is via `Assets/Plugins/x86_64` native DLL.

---

## Troubleshooting checklist

- Started Play Mode from the wrong open scene → open **AppRoot** and press Play again.
- Missing platform TTS at runtime:
  - check Plugins folder for that platform
  - check plugin import settings per platform
- UI speech doesn’t play:
  - verify `UiAudioOrchestrator.Init(...)` gets called from bootstrap
  - check `ISpeechService` implementation for that platform
