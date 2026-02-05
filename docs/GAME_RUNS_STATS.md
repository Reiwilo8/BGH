# Game runs, parameters, seeds, stats

## Run context (lifecycle)

`IGameRunContextService` tracks the current run:
- `PrepareRun(gameId, modeId, seed?, initialParameters?, wereRunSettingsCustomized?)`
- `StartRun()`
- `PauseRun()` / `ResumeRun()`
- `FinishRun(reason, completed, score, finishedUtc?, runtimeStats?)`
- events: `RunPrepared`, `RunStarted`, `RunFinished`

Duration is computed as:
- (finished - started) minus accumulated paused time.

> Note: `score` is currently always 0 (placeholder by design).

---

## Initial parameters (run-time configuration)

`GameRunInitialParametersBuilder` builds a dictionary including:
- seed mode (Random/Fixed)
- seed value (if fixed)
- effective control hint mode (Auto → platform preferred)
- game volume

Game-specific providers can append additional keys:
- `CompositeGameInitialParametersProvider`
- `MemoryInitialParametersProvider`
- `PersistentGameInitialParametersProvider` (store-backed)

Memory uses:
- `memory.boardWidth`
- `memory.boardHeight`
with mode defaults (tutorial/easy/medium/hard/custom) and persistence.

---

## Seed preferences (per-game)

`IGameRunParametersService` persists:
- random vs fixed seed mode
- selected seed (optional)
- MRU list of known seeds (capped to 50)

`GameRunSeedHistoryReporter` listens to `RunPrepared` and stores seeds used.

---

## Persistent stats

`IGameStatsService` stores per-game/per-mode:
- runs, completions
- best completed time (min), best survival time (max)
- last played UTC
- recent run history (duration, completed, score, runtimeStats)

History caps:
- internal store: 50 entries
- UI can apply `GameStatsSnapshotLimiter.LimitRecent(snapshot, capacity)`
- `IGameStatsPreferencesService` stores user “recent capacity” (1–10)
