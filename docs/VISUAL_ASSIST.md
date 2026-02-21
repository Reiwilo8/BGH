# Visual Assist (VA) + Visual Mode

## Terminology
- **Visual Mode**: “is visual UI enabled?” (`AudioOnly` vs `VisualAssist`)
- **Visual Assist**: the concrete HUD implementation (text + dimmer + hints)

---

## VisualModeService
- Holds current `VisualMode`.
- `ToggleVisualAssist()` switches `AudioOnly` ↔ `VisualAssist`.
- Mode is set on boot from `ISettingsService` and can be changed by UI.

---

## VisualAssistService (state model)
Exposes a minimal UI state:
- Header, SubHeader
- CenterText (composed from prioritized layers)
- DimAlpha01
- Transitioning flag
- List move pulse + direction
- Root visibility

### Center layering
The “center” text chooses one visible layer at a time:
- Gesture (highest)
- Transition
- IdleHint
- PlannedSpeech
…with rules that avoid conflicting messaging.

### Idle hint vs repeat flash
- `EvaluateIdleHint(canShow, idleSeconds)` decides whether to show:
  - a repeat prompt,
  - a custom idle hint,
  - or nothing.

### Marquee gate (optional)
Some long texts can require at least one marquee pass before audio proceeds.
The gate is implemented via `IVisualAssistMarqueeGate`.

---

## Where VA/VM is toggled
- Start screen: gesture + F1 (desktop)
- Hub settings: toggle setting
- GameModule does not operate VA/VM directly
