# Scenes

Ordered list used by the project:

1. AppRoot
2. StartScene
3. HubScene
4. GameModuleScene
5. SteamRushScene
6. FishingScene
7. MemoryScene

---

## Roles (conceptual)

### AppRoot
- Global bootstrap & services.
- No gameplay/UI “content” beyond core systems.

### StartScene
- First interactive UI.
- Supports Visual Assist toggle (gesture + F1 on desktop).

### HubScene
- Main navigation hub.
- Contains global settings (including Visual Mode toggle setting).

### GameModuleScene
- “Pre-run” shell for a selected game:
  - run settings (seed/random, mode)
  - game stats browsing (“GameStatsState”)
- Does **not** own Visual Mode/Visual Assist toggling (that lives in Start/Hub).

### Gameplay scenes
- **MemoryScene**: implemented gameplay
- **SteamRushScene**: placeholder (no gameplay)
- **FishingScene**: placeholder (no gameplay)

---

## Additive loading model

- `AppRoot` stays loaded (persistent).
- Other scenes are loaded/unloaded additively by flow.
- Content scenes should not assume they are the only scene loaded.
