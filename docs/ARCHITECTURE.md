# Architecture

## AppRoot as the “logical main scene”

- `AppRoot` contains `AppRootBootstrap` (DefaultExecutionOrder -10000).
- The GameObject is marked `DontDestroyOnLoad`, so it persists across additive loads.
- Every other scene is treated as “UI/game content” that can be loaded/unloaded without losing global state.

### Why this pattern?
- Global services (settings, TTS, localization, input normalization, haptics, audio cues) must be available everywhere.
- Additive scenes remain lightweight and focused on their UI/game responsibilities.

---

## Service locator (lightweight)

- `ServiceRegistry` stores instances by concrete type key.
- `AppContext.Services` resolves the registry from `AppRootBootstrap` and caches it.

**Notes**
- This is intentionally simple: it avoids container config complexity.
- Domain reload in Editor is handled via `AppContext.ResetCacheForDomainReload()`.

---

## Boot sequence (high level)

`AppRootBootstrap.Awake()`:
1. Creates `ServiceRegistry` + registers:
   - `AppSession`
   - `ISettingsService` (PlayerPrefs-backed)
   - `ISpeechService` (platform factory)
   - `ILocalizationService` (Unity Localization)
   - `ISpeechLocalizer`
   - `IVisualModeService`
   - `IVisualAssistService`
   - `IAudioFxService`
   - `IHapticsService`
   - `IUiAudioOrchestrator`
   - `IAppFlowService`
   - `IUserInactivityService`
   - `IInputService`
   - `IInputFocusService`
   - `IRepeatService` + `RepeatAutoDriver`

`AppRootBootstrap.StartAsync()`:
- Plays `WelcomeChime`
- Speaks localized `"app.welcome"` via orchestrator (non-interruptible)
- Enters Start scene (`flow.EnterStartAsync()`)

---

## Flow responsibility

- `IAppFlowService` controls moving between: Start → Hub → GameModule → Gameplay scene.
- Voice and VA updates should generally happen via orchestration (see `AUDIO_TTS.md`, `VISUAL_ASSIST.md`).
