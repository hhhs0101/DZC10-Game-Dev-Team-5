# Unity Defense — 3D Tabletop Prototype

A Unity **6000.6.0f1** project that extends the existing combat prototype with a session-based Lobby, a six-slot Deck Editor, and a three-card gameplay Hand shared by towers and skills. Decks persist to disk as three independent presets; Gold and Candy also persist. Final graphics and progression saves are not implemented.

## Current Migration Status

The Test Stage now uses a fixed front-facing perspective camera, a 3D table, Sphere enemies, and Cuboid towers. Gameplay stays planar on **XZ**; height does not affect range, targeting, movement speed, or skill radius.

The gameplay scene uses the supplied kitchen illustration behind the 3D table. `KitchenBackground.png` and its unlit material are referenced by a camera-owned `Kitchen Background` quad. `CameraBackdrop` fills the viewport without stretching (cropping edges when needed). The background has no collider, casts no shadows and uses Ignore Raycast; it does not participate in combat or input. Replace the material texture to change the illustration.

**Tower mouse placement now works in 3D.** Select a Tower Card, hover an available green slot, and click to build. Slot-only 3D raycasts ignore table/enemy colliders and respect UI blocking. Failed placement preserves your card and resources. **Skill mouse aiming now works on the 3D tabletop.** Select a Skill Card, press and hold LMB on the table, move to aim, then release to cast. Releasing over UI or outside the table cancels.

Implementation reports and previews are stored locally in `Documentation/Report/` and excluded from Git. User-provided specifications are versioned in `Documentation/Prompt/`.

## Getting Started and Controls

1. Open the `GameDesign` folder in Unity Hub, open `Assets/Defense/Scenes/MainMenu.unity`, and enter Play Mode.
2. Select **Start Game → Lobby**. Navigate with **A / D** or click the left or right stage card. Holding a key repeats navigation every 0.15 seconds after an initial 0.4-second delay.
3. Once the selected stage is centered, press **Enter** or click **Enter Stage**. The list contains stages 1-1 through 1-10 and an always-unlocked **Test Stage** at the right end. Only campaign stage 1-1 is initially unlocked.
4. Open **Deck** and drag a tower or skill card from the left page onto a T1–T6 slot on the right. Use **Preset 1 / Preset 2 / Preset 3** to switch decks in the Deck Editor. The Lobby only previews the selected deck. Valid edits and the active selection save immediately. Select **Back to Lobby** to return.
5. Enter the stage to observe enemies moving from the right side of the tabletop toward the Base on the left. Select a Tower Card and click a green slot to place it. Successful placement pays costs and rotates the common Hand/Queue. For skills, hold LMB on the table to aim and release to cast; release over a card or any other UI to cancel.
6. Use **Pause / Esc** to pause or resume. Your deck is preserved when returning through Pause → Exit → Yes or Game Over → Lobby. **Retry** on the Game Over screen starts a fresh run of the same stage. The finite Test Stage shows **Stage Clear → Replay / Return to Lobby** on success.

Settings is a placeholder screen. Closing it returns to the screen that opened it: Main Menu, Lobby, Deck Editor, or Pause.

## Data and Current Assumptions

- `Assets/Defense/Resources/GameCatalog.asset` defines stage order, the tower and card catalogs, and the initial T1–T6 assignments.
- There are 15 tower types. The initial deck is `[Test1, Test2, Test3, Test4, Test5, Test6]`. PlayerDeck always contains six unique CardDefinition references, including at least one Tower Card.
- Available Cards is **the full card catalog minus the six cards in the current deck**. There are 17 cards (15 towers and 2 skills), so 11 are available. After replacement, the incoming card disappears and the outgoing card returns immediately. Duplicate assignments and removing the final Tower Card are rejected.
- T1–T6 are persistent deck-editing positions. At stage entry, a separate runtime copy is shuffled once. Hand and Queue changes do not modify PlayerDeck.
- Three independent deck presets and the active preset index survive application restarts. Progression and stage selection remain session-only. No PlayerPrefs are used.
- Stages `1-1` through `1-10` all reuse the TestStage map. Starting resources increase from 150 to 375, while enemy spawn intervals decrease from 2 seconds to 1.1 seconds.
- `PlayerSession.CompleteActiveStage()` remains an idempotent progression-only operation. Finite stages call it on Victory; repeat clears leave completion unchanged and still award Gold. Game Over does not unlock stages. The ten existing campaign test configurations still spawn continuously; enable Finite Spawning and configure Total Enemies on their StageDefinition assets to give them a clear condition.

## Deck Persistence and Presets

`PlayerSession.Deck` exposes `DeckPresetManager.ActiveDeck`; there is no separate active-deck copy. Each of the three presets is an existing PlayerDeck with six unique cards and at least one Tower Card. Switching immediately refreshes slot labels, Available Cards, selected-button styling, and Lobby preview, and cancels any in-progress drag. Combat still copies the active deck once per run; shuffling, Hand cycling and Retry never save battle state.

The save is `deck-presets.json` in `Application.persistentDataPath`. Its JSON contains `Version: 1`, a zero-based `ActivePresetIndex`, and three `Presets`, each containing six ordered `CardIds`. CardCatalog resolves the existing stable CardDefinition.CardId values; display names and catalog order are irrelevant. Keep IDs stable when renaming content.

First launch creates and saves three independent copies of GameCatalog.InitialDeck. Each loaded preset is validated through PlayerDeck. An unknown/missing ID, duplicate or invalid composition resets only that preset, preserving valid siblings. Malformed files or unsupported versions restore defaults with warnings. Invalid active indices reset selection to Preset 1. Repairs are saved immediately.

DeckPersistence delegates file I/O to the shared JsonFileStore, which writes and flushes a temporary file, then replaces the old save. A write failure retains both the in-memory edit and previous save and logs that persistence failed; it does not silently report success. The next valid edit or preset change attempts to save the complete current state again. Saves include only deck composition and active preset selection, not progression, Elixir, stage state or battle order.

No Scene, Prefab or Inspector wiring is required. The existing UI builders create the preset buttons. Unity's built-in JSON serialization module is enabled; no external package was added.

## Stage Clear, Gold, and Candy

The dedicated **Test Stage** reuses the existing TestStage scene and is accessible at the right end of the Lobby carousel without campaign progression. It starts with 3 Base HP and 300 resources, spawns exactly five enemies at 0.75-second intervals, and awards 50 Gold on every clear. Its RewardTestEnemy definition uses 20 HP, speed 3, Base damage 1, resource reward 10, and a 100% chance of 1 Candy per combat kill. Let enemies through to test failure. BasicEnemy uses a 10% chance of 1 Candy. These are editable asset values, not hard-coded reward rules.

`FixedIntervalSpawner` counts successful spawns and exposes completion. `EnemyRegistry` owns the active population. `StageVictoryController` evaluates finished spawning plus zero active enemies in LateUpdate, after the final enemy's Base damage callback finishes. `GameFlow.Victory` is terminal, mutually exclusive with Game Over, and freezes gameplay. Progression completion is separate from the once-per-run Gold award. Replay uses the existing scene reload, resetting the runtime Hand, Elixir, towers, skills, enemies and schedule while retaining currency, presets and session progression.

`PlayerSession.Currency` owns one `PlayerCurrency`. Gold and Candy are separate from stage resources and Elixir. Controlled add/spend operations reject negative amounts, unaffordable spending and integer overflow. `CandyDropSystem` subscribes to the spawner's combat-death event and rolls once using that enemy's definition. Base arrival and arbitrary object removal do not award Candy. Successful drops save immediately and survive Game Over; no pickup input or visual is required. Lobby and gameplay counters subscribe to currency changes.

`player-currency.json` under `Application.persistentDataPath` stores only `Version: 1`, `Gold` and `Candy`. The shared JsonFileStore also serves deck saves. Currency changes save immediately using a flushed temporary file and replacement; failure retains memory and the previous file, logs an explicit error, and exposes `LastSaveSucceeded = false`. A later valid mutation retries the complete current state. Malformed/unsupported saves reset balances with a warning; negative loaded balances are clamped and saved. Stage completion/unlocks remain **session-only** and are not restored after application restart.

No manual Scene/Prefab rewiring is required: StageInitializer adds and connects the reward components, and existing UI builders create the counters and Victory panel. No new package, shop, wave framework, pickup object or final visual asset is introduced.

## Default Statistics

| Tower | Damage | Range | Attacks per Second | Cost |
|---|---:|---:|---:|---:|
| Test Turret 1 | 10 | 3 | 1 | 50 |
| Test Turret 2 | 30 | 5 | 0.4 | 75 |
| Test Turret 3 | 5 | 2.5 | 3 | 100 |

Stage 1-1 starts with 10 Base HP, 150 resources, and a 2-second enemy spawn interval. The basic enemy has 40 HP, a movement speed of 1.5, Base damage of 1, and a kill reward of 10 resources. Edit **Resource Reward** in `BasicEnemy.asset` to configure the reward for that enemy definition.

Green placement slots have available, highlighted, and occupied presentation states. Available slots highlight when hovered with a Tower Card selected. HP bars appear above the red enemies. All 15 tower types share the existing BasicTurret prefab and combat code. Scroll the left page of the Deck Editor to view additional cards. All tower statistics are configurable in the corresponding definition assets.

## Cards, Elixir, and Skills

`CardDefinition` holds a stable ID, display name, placeholder color, and Elixir cost. `TowerCardDefinition` references the existing `TurretDefinition`; `SkillCardDefinition` references a `SkillDefinition`. Combat remains outside the UI.

Every run starts with **3 Elixir**, regenerates **0.36 per second** while Playing, and caps at **10**. Pause, Game Over and Victory stop regeneration. The vertical bar on the right smoothly follows the authoritative value. Retry resets it.

| Skill | Damage | Radius | Casting Time | Travel Time | Elixir |
|---|---:|---:|---:|---:|---:|
| Toy Throw | 60 | 1.5 | 0 s | 0 s | 3 |
| Slow Area | 0 | 3 | 0 s | 0 s | 2 |

Toy Throw reuses the circular damage strategy and Enemy damage/reward API. Slow Area creates an 8-second zone with a 0.5 movement multiplier, followed by a separate 0.5-second visual fade. Leaving a zone removes its slow immediately; overlapping zones retain independent sources and use the strongest slow once, without multiplication. Enemy base speed is unchanged.

Camera rays accept only the configured GameplaySurface collider and reject UI. One SkillDefinition.Radius drives targeting, effects, and placeholder circles. Slow membership uses XZ distance queries and movement/registry notifications, independent of visuals and enemy height.

Both skills share the existing Hand/Elixir commitment pipeline. Empty-area casts still pay and cycle. Cancellation, invalid placement, or insufficient funds never spend Elixir or advance the cycle. CastingTime (before launch) and TravelTime (after launch) are separate delays; no projectile is rendered. The optional ThrowOrigin is captured separately from the target. Pause freezes both delays and area/fade timers; Game Over and Victory discard pending casts and freezes the scene until Retry/exit destroys its runtime objects.

Tower Cards currently cost **0 Elixir** and retain their existing resource costs. Their Elixir cost can be changed independently in the card asset. The default deck remains the original six towers; add Toy Throw and Slow Area through the Deck Editor.

Existing skill GUIDs and stable card IDs (`skill.fireball`, `skill.arrowrain`) are retained for reference compatibility; display names and asset paths reflect the new content.

Skill development values are editable in the corresponding SkillDefinition assets. Slow Area uses ActiveDuration, FadeDuration and SlowMultiplier; Radius is shared by logic and presentation. The TestStage SkillCastingController already references its TabletopSurface, whose camera, GameplaySurface layer mask and BoxCollider provide targeting; no new scene wiring or packages are required. An optional Throw Origin may be assigned on SkillCastingController for future projectiles.

New data lives in `Assets/Defense/Data/TowerCard*.asset`, `ToyThrowCard.asset`, `SlowAreaCard.asset`, and the corresponding skill definitions.

## Documentation and Git configuration

- [Documentation/Prompt/](Documentation/Prompt/): user-provided specifications and implementation prompts, tracked in Git.
- `Documentation/Report/`: local implementation reports, architecture/setup notes, validation results, and previews. This directory is excluded from Git; it will not be included in a fresh clone.
- Future assistant reports and generated validation results belong in `Documentation/Report/`.
- Prompts use English prose. Original Korean UI-message requirements are retained as Unicode escapes beside their English translations; this does not change runtime localization.
- `.gitignore` excludes generated Unity files and local reports. Keep Assets (including .meta), Packages, and ProjectSettings tracked.
- `.gitattributes` defines text normalization and binary handling. Git LFS and custom Unity merge drivers are not enabled by this configuration.

The project uses Unity's built-in uGUI and modules. No additional external packages have been added. Keep `.meta` files alongside their corresponding assets.

## Automated Validation

Use **Defense → Run Prototype Validation (Play Mode)** outside Play Mode. The existing suite includes SkillInteractionValidation for real camera/physics/UI raycasts, cancellation, Elixir and card cycling, AoE boundaries, separate cast/travel delays, and source-aware slow enter/exit, overlap, lifetime, fade, and cleanup. DeckPersistenceValidation additionally covers preset independence, autosave/recovery/failure handling, actual UI switch/drop events, and active-preset stage/Retry integration. RewardValidation covers finite spawning, deferred Victory, duplicate protection, two clears, real Replay/Retry scene reloads, final lethal Base arrival, deterministic drop probabilities, currency notifications, disk recovery and write failures. The regression runner redirects deck and currency storage to a unique temporary path to protect player saves. Separate `RewardValidation.WriteRestartFixture` and `ReadRestartFixture` batch entry points validate currency restore across two Unity processes; set `DEFENSE_TEST_DECK_SAVE_PATH` to an isolated file in the same temporary directory for both runs. Reports are written to the ignored Documentation/Report directory. Automated pointer-coordinate tests are not manual mouse testing.
