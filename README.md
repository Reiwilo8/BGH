# Blind Games Hub (Unity)

A multiplatform application (priority: **mobile**, target also **Windows .exe**) developed in **Unity 6.0 LTS**, designed primarily for **blind and visually impaired users**.

The project follows an **audio-first design philosophy**:
- text-to-speech (TTS),
- spatial and signal-based audio,
- touch gestures and device sensors,
- a minimalist, optional GUI.

---

## Core Principles

- **No GUI dependency** - the application is fully usable without any graphical interface.
- **System-level, offline TTS**:
  - Android: TextToSpeech
  - iOS: AVSpeechSynthesizer
  - Editor (Windows): simulated (logs + demo GUI)
- **Offline-first** - no cloud services required.
- **Modular game architecture**:
  - each game is a separate scene + ScriptableObject (`GameDefinition`),
  - individual difficulty levels and game-specific settings.
- **Single root scene (`AppRoot`)**:
  - all other scenes are loaded additively.

---

## Scene Architecture

```
AppRoot (always loaded, DontDestroyOnLoad)
└─ StartScene (application entry, GUI toggle, exit)
└─ HubScene (main menu, game selection, global settings)
└─ GameScene_X
└─ Game menu (tutorial / difficulty / game settings)
└─ Gameplay
```

- The build always starts in **AppRoot**.
- `StartScene` is loaded additively and can be revisited.
- Exiting the application bypasses `StartScene` and quits directly.

---

## Input & Navigation (High-Level)

### StartScene / Hub / Selectors
- **Swipe (right / down)** – next item
- **Swipe (left / up)** – previous item
- **Double-tap** – confirm / enter
- **Long-press** – back / pause / return to StartScene
- **Two-finger tap** – toggle GUI (StartScene only)

### Gameplay
- Input gestures are **defined per game**.
- There is **no requirement** to use global navigation gestures during gameplay.

---

## Project Structure

```
Assets/
├─ _Project/
│ ├─ Core/ # shared logic (pure C#, no UnityEngine dependency)
│ ├─ Hub/ # hub states and navigation logic
│ ├─ Games/ # game modules
│ └─ UI/ # minimalist demo GUI
├─ Scenes/ # AppRoot, StartScene, HubScene, GameScene_X
├─ Plugins/
│ ├─ Android/
│ └─ iOS/
└─ Settings/ # render pipeline & scene templates (Unity-generated)
```

- `_Project` contains **only project-specific code and assets**.
- The `Settings` folder is generated/used by Unity (URP, scene templates) and is not part of the application core.

---

## Text-to-Speech (TTS) Architecture

Unity does **not** provide a single cross-platform TTS API.  
Therefore, the application uses a strict abstraction:

```csharp
public interface ISpeechService
{
    void Speak(string text, SpeechPriority priority = SpeechPriority.Normal);
    void StopAll();
    void SetLanguage(string languageCode);
    bool IsSpeaking { get; }
}
```

- All application logic (Hub, games) communicates **only through this interface**.
- Platform-specific implementations are selected centrally via compilation flags.
- The core logic has **no direct dependency on native TTS APIs**.

This allows TTS implementations to be added or replaced without refactoring the application logic.

---

## Localization

- **No hardcoded user-facing strings** exist in the code.
- All text (including TTS output) is referenced via localization keys.
- The project uses **Unity Localization Package**.
- Changing the language:
  - updates localization tables,
  - updates the TTS language via `ISpeechService.SetLanguage`.

---

## GUI (Demonstration Mode)

- The GUI is **optional** and not required for normal operation.
- Its purpose:
  - demonstrations,
  - development/testing,
  - tuning parameters (timings, sensitivities),
  - alternative/easier gameplay (for users without severe visual impairment).
- GUI can be enabled/disabled:
  - in **StartScene** via gesture (two-finger tap),
  - or later via settings.