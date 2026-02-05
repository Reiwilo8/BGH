# Input

## Goals
- Unify diverse input sources into a small, app-oriented vocabulary:
  - `NavAction`: Next / Previous / Confirm / Back / ToggleVisualAssist / ...
  - `NavDirection4`: Up/Down/Left/Right (optional signals for UI semantics)

---

## Input sources

### Keyboard + Mouse (desktop)
- `KeyboardMouseInputSource` reads from an `InputActionAsset`:
  - map: `"Navigation"`
  - actions: `Next`, `Previous`, `Confirm`, `Back`, `ToggleVisualAssist`, `Repeat`, `Scroll`, optional `Up/Down/Left/Right`
- Scroll emits next/previous with cooldown.

### Touch gestures (mobile)
- `TouchGestureInputSource` (EnhancedTouch):
  - Swipe: emits Next/Previous + a Direction4 hint
  - Single tap: triggers *Repeat* after delay (if not double-tap)
  - Double tap: Confirm
  - Long press: Back (also drives a Visual Assist dimmer ramp)
  - Two-finger tap: Toggle Visual Assist (future-proofed; cancelled by pinch delta)

Gesture tunables live in `GestureSettings` ScriptableObject.

---

## Focus (scope)

`IInputFocusService` keeps a stack:
- Start
- Hub
- GameModule
- Gameplay

This lets systems/UI decide whether to act on input in a given context.

---

## Repeat (user help)

`IRepeatService`:
- Manual: space key or single tap (touch)
- Auto: after inactivity threshold and if allowed by settings
- Guard rails:
  - won’t repeat while TTS is speaking
  - won’t repeat during flow transitions
