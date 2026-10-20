# 2D Top-Down Migration — Stage 3: Full Gameplay and Persistence Integration

## Objective

Connect the validated native 2D battlefield to the existing **real game session, UI, deck/Hand, Elixir, stage clear, Gold/Candy rewards and persistence**. Preserve all current game rules and save data.

Prerequisites: Stage 1 and Stage 2 acceptance gates passed. Read `2D_TopDown_Migration_Overview.md`.

## 1. Integrate the Existing Game Flow

The 2D test battlefield must participate in the existing navigation flow rather than creating a second independent game:

```text
Start / Lobby
-> Stage Select
-> (selected Stage, active Deck preset)
-> 2D Battlefield
-> Playing / Paused
-> Victory or Game Over
-> Replay / Return to Lobby
```

Reuse the current scene/session bootstrap where possible. Avoid a parallel `PlayerSession`, duplicate Stage catalog, or distinct currency wallet for 2D.

During integration, keep the original 3D scene accessible as a fallback until Stage 4 is complete; do not make the new 2D scene the sole viable entry point prematurely.

## 2. Deck, Presets and Runtime Hand

- Use the existing authoritative persistent Deck / preset source; no reinitialized default Deck when entering the new scene.
- Preserve three presets and active selection.
- Preserve six unique cards and the rule of at least one Tower Card.
- Create runtime copy, shuffle once, populate three-card Hand and UpcomingQueue using existing behavior.
- Successful Tower placement or Skill cast cycles the used Card; cancellation/failure does not.
- Runtime card rotation must never mutate persisted preset composition.
- Preserve preset switching in the Deck Lobby/Editor, outside gameplay as previously designed.

## 3. Elixir and Other Combat Resources

- Reconnect the existing resource and Elixir UI without changing balance.
- Existing Elixir baseline: start 3, maximum 10, regenerate 0.36/sec while Playing; pause/game over stop regeneration.
- Apply current Card Elixir costs exactly once on successful use; zero-cost Towers remain supported.
- Retry resets temporary Elixir to the configured start value.
- If other combat resources currently exist, preserve their established rules rather than removing them silently.

## 4. Enemy Spawning and Stage Completion

- Reuse the existing finite-spawn and alive-enemy-tracking mechanisms, but instantiate 2D-compatible Enemy prefabs at 2D map-defined spawn/waypoint locations.
- Preserve actual Stage Clear rules rather than using an artificial timer.
- Preserve the current authoritative stage-clear policy. A finite-stage baseline may check `SpawnerFinished && AliveEnemyCount == 0`, but **this alone does not prove that every Enemy was killed** if Enemies can reach the Base and despawn. For the five-Enemy Test Stage, preserve the requirement that all five enemies are actually defeated; do not count reaching the Base as a kill. Inspect the existing clear condition before changing it.
- Victory and Game Over are mutually exclusive terminal states; reward processing once per run.
- Retain first-clear progression and later stage unlock logic; repeats do not reapply unlock side effects.
- Preserve the five-Enemy repeatable Test Stage in the new 2D scene.

## 5. Gold Reward

- Award the Stage's configured fixed Gold amount on **every** successful clear, including repeats.
- Never grant clear Gold on Game Over or on every frame after clear.
- Keep `CompleteActiveStage()`/progression and reward payment as separate responsibilities.
- Display Gold earned in the existing Victory UI or its adapted 2D-compatible screen-space UI.

## 6. Candy Drop and Retention

- Preserve Enemy-definition-specific `CandyDropChance` and `CandyDropAmount`.
- Roll only for legitimate combat death, at most once per Enemy.
- Add Candy immediately upon a successful roll; no pickup click required.
- Candy acquired before failure must remain persistently owned after Game Over.
- If Candy drop animation exists, adapt it to the 2D world as **presentation only**; acquisition may not depend on the visual.
- Continue using authoritative currency and save service, not a 2D-scene-local wallet.

## 7. UI Compatibility

Reuse/reconnect rather than redesign:

- Hand + Card selection visuals
- Elixir bar and existing resources
- HP/base health and Enemy HP bars
- Pause screen and input
- Game Over UI
- Victory UI and clear Gold reward display
- Lobby/Stage Select/Deck Editor
- Gold and Candy totals where currently displayed

Screen-space UI should stay screen-space. Guard 2D world input against UI clicks and releases.

## 8. Replay and Stage Reset

On new run/Replay, reset **only transient state**:

```text
Spawner schedule and counters
Alive Enemy registry
Enemy/Tower instances
Placement slot occupancy
Skill effects / active slow sources
Temporary HP, resources and Elixir
Victory / Game Over / Pause state
Runtime Hand and queue
```

Do not reset:

```text
Persistent Deck presets and active preset
Gold
Candy
Saved completed/unlocked stage progression
Future persistent upgrades (if already implemented)
```

Ensure old stage objects/listeners do not leak across scene reload or Retry. Correctly unsubscribe/rebind events where necessary.

## 9. Save Compatibility and Non-Destructive Load

This migration must **not** rename or discard Card IDs, Stage IDs, preset save files, currency save files or other player-progress data.

- Inspect existing storage format and initialization order.
- Do not reset user data simply because the new scene uses a different root prefab or stage-map asset.
- Avoid unintended `DefaultDeck` assignment or creation of a new empty player profile.
- If an existing saved stage references old map geometry, map that **stage identifier** to an appropriate 2D map configuration without changing progression semantics.
- If compatibility code is needed, make it explicit and test with a *copy* of stored data.
- If migration uncovers a pre-existing persistence issue, report it separately rather than silently rewriting the save format.

## 10. Scope Exclusions

Do not implement:

- new progression or economy balancing
- new shop/upgrade mechanics
- Tower merge, TFT-style synergy, counter-type system
- new projectile combat
- permanent removal of the 3D scene
- final art and VFX

## 11. Acceptance Gate

Verify end-to-end:

1. Lobby -> Stage Select -> 2D stage loads correctly with chosen stage and active Deck preset.
2. Six-card Deck/three-card Hand cycles by existing rules.
3. Tower placement and both Skill interactions remain fully usable.
4. Elixir start/regen/pause/spend/retry behavior remains unchanged.
5. Exactly five enemies spawn in the repeatable Test Stage; no sixth appears.
6. Defeating all five triggers Victory exactly once. Letting an Enemy reach the Base must not accidentally satisfy a five-kill objective.
7. First clear updates progression; replaying grants fixed Gold again without duplicate unlock side effects.
8. Game Over grants no clear Gold.
9. Enemy-specific Candy drops are automatic, persist and survive a failed run.
10. Victory UI, Game Over UI, Pause and Retry remain operational.
11. Deck presets, chosen preset, Gold, Candy and progression survive full application restart when those data are persisted by the current implementation.
12. Save files are not reset/overwritten by starting the new 2D scene.
13. At least one full Replay returns to a clean stage with no residual spawned enemies, slow zones, occupied slots, or listeners.

Record actual Play Mode and restart results distinctly from automated/code-level tests. **Do not move the default gameplay route over permanently until these conditions pass or their exceptions are explicitly accepted.**
