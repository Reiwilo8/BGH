# Design Decisions

This document outlines key architectural and design decisions made during development.

---

## Audio-First Architecture

The application prioritizes audio interaction to ensure full accessibility for blind users.
All gameplay and navigation logic is independent of visual rendering.

---

## Offline-First Approach

No cloud services are required.
All speech synthesis relies on system-level TTS engines available on the target platform.

---

## Score as a Placeholder

The run system supports scoring, but current games report a constant score value (`0`).

Rationale:
- Score has limited value in accessibility-focused research
- Time, completion, and consistency are more meaningful metrics
- System remains extensible for future scoring implementations

---

## Independent State Machines

The application intentionally separates:
- Hub state machine
- Game module state machine
- Gameplay runtime logic

These systems are decoupled and communicate only through well-defined services.

---

## Visual Assist as a Support Layer

Visual Assist is implemented as:
- Optional
- Minimalist
- High-contrast
- Non-blocking

It is not required for normal operation and does not alter core gameplay logic.