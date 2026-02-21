# Blind Games Hub

**Blind Games Hub** is a multiplatform application developed in **Unity 6.0 LTS (6000.0.x)**, designed primarily for **blind and visually impaired users**.

The project follows an **audio-first design philosophy**, where all core interactions are accessible without relying on a graphical user interface. Visual elements are optional and implemented as a supportive layer rather than a primary interaction channel.

Target platforms:
- **Android** (primary)
- **iOS**
- **Windows (Standalone)**
- macOS / Linux (experimental, without native TTS)

---

## Core Design Principles

- **Audio-first interaction model**
- **No mandatory GUI dependency**
- **Offline-first architecture**
- **System-level Text-to-Speech (TTS)**
- **Strict separation of concerns**
- **Modular game architecture**

The application is fully operable using audio feedback, gestures, and keyboard input, with visual support available as an optional assistive mode.

---

## Project Structure

```
Assets/
├─ _Project/
│ ├─ Core/ # Platform-agnostic core logic
│ ├─ Hub/ # Hub navigation and states
│ ├─ Games/ # Game modules
│ └─ UI/ # Visual Assist implementation
├─ Scenes/
│ ├─ AppRoot
│ ├─ StartScene
│ ├─ HubScene
│ ├─ GameModuleScene
│ ├─ MemoryScene
│ ├─ SteamRushScene
│ └─ FishingScene
├─ Plugins/
│ ├─ Android/
│ ├─ iOS/
│ └─ x86_64/
```

---

## Scenes Overview

See: **SCENES.md**

---

## Input & Navigation

See: **INPUT.md**

---

## Text-to-Speech Architecture

See: **AUDIO_TTS.md**

---

## Visual Mode & Visual Assist

See: **VISUAL_ASSIST.md**

---

## Game Runs & Statistics

See: **GAME_RUNS_STATS.md**

---

## Available Games

See: **GAMES.md**

---

## Building & Installation

See: **BUILDING.md**

---

## Additional Documentation

- **ARCHITECTURE.md** – system-level architecture
- **DESIGN_DECISIONS.md** – rationale behind key design choices
- **LIMITATIONS.md** – known limitations and constraints

---

## License

This project is developed for research and educational purposes.