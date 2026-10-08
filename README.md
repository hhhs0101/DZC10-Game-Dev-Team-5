# Unity Defense — 3D Tabletop Skills

A Unity **6000.6.0f1** project that extends the existing combat prototype with a session-based Lobby, a six-slot Deck Editor, and a three-card gameplay Hand shared by towers and skills. Final graphics and disk persistence are not implemented.

## Current Migration Status

The Test Stage now uses a fixed front-facing perspective camera, a 3D table, Sphere enemies, and Cuboid towers. Gameplay stays planar on **XZ**; height does not affect range, targeting, movement speed, or skill radius.

**Tower mouse placement now works in 3D.** Select a Tower Card, hover an available green slot, and click to build. Slot-only 3D raycasts ignore table/enemy colliders and respect UI blocking. Failed placement preserves your card and resources. **Skill mouse aiming now works on the 3D tabletop.** Select a Skill Card, press and hold LMB on the table, move to aim, then release to cast. Releasing over UI or outside the table cancels.

Implementation reports and previews are stored locally in `Documentation/Report/` and excluded from Git. User-provided specifications are versioned in `Documentation/Prompt/`.

## Getting Started and Controls

1. Open the `GameDesign` folder in Unity Hub, open `Assets/Defense/Scenes/MainMenu.unity`, and enter Play Mode.
2. Select **Start Game → Lobby**. Navigate with **A / D** or click the left or right stage card. Holding a key repeats navigation every 0.15 seconds after an initial 0.4-second delay.
3. Once the selected stage is centered, press **Enter** or click **Enter Stage**. The list contains stages 1-1 through 1-10; only 1-1 is initially unlocked under the existing progression rules.
4. Open **Deck** and drag a tower or skill card from the left page onto a T1–T6 slot on the right. Changes apply immediately. Select **Back to Lobby** to return.
5. Enter the stage to observe enemies moving from the right side of the tabletop toward the Base on the left. Select a Tower Card and click a green slot to place it. Successful placement pays costs and rotates the common Hand/Queue. For skills, hold LMB on the table to aim and release to cast; release over a card or any other UI to cancel.
6. Use **Pause / Esc** to pause or resume. Your deck is preserved when returning through Pause → Exit → Yes or Game Over → Lobby. **Retry** on the Game Over screen starts a fresh run of the same stage.

Settings is a placeholder screen. Closing it returns to the screen that opened it: Main Menu, Lobby, Deck Editor, or Pause.

## Data and Current Assumptions

- `Assets/Defense/Resources/GameCatalog.asset` defines stage order, the tower and card catalogs, and the initial T1–T6 assignments.
- There are 15 tower types. The initial deck is `[Test1, Test2, Test3, Test4, Test5, Test6]`. PlayerDeck always contains six unique CardDefinition references, including at least one Tower Card.
- Available Cards is **the full card catalog minus the six cards in the current deck**. There are 17 cards (15 towers and 2 skills), so 11 are available. After replacement, the incoming card disappears and the outgoing card returns immediately. Duplicate assignments and removing the final Tower Card are rejected.
- T1–T6 are persistent deck-editing positions. At stage entry, a separate runtime copy is shuffled once. Hand and Queue changes do not modify PlayerDeck.
- Deck and progression state persist for the current application session. Restarting the application restores the initial deck and progression. No save files or PlayerPrefs are used.
- Stages `1-1` through `1-10` all reuse the TestStage map. Starting resources increase from 150 to 375, while enemy spawn intervals decrease from 2 seconds to 1.1 seconds.
- Calling `PlayerSession.CompleteActiveStage()` unlocks the next stage. No victory condition has been added to the continuous-spawning prototype, so normal gameplay does not automatically complete stages. Game Over does not unlock stages.

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

Every run starts with **3 Elixir**, regenerates **0.36 per second** while Playing, and caps at **10**. Pause and Game Over stop regeneration. The vertical bar on the right smoothly follows the authoritative value. Retry resets it.

| Skill | Damage | Radius | Casting Time | Travel Time | Elixir |
|---|---:|---:|---:|---:|---:|
| Toy Throw | 60 | 1.5 | 0 s | 0 s | 3 |
| Slow Area | 0 | 3 | 0 s | 0 s | 2 |

Toy Throw reuses the circular damage strategy and Enemy damage/reward API. Slow Area creates an 8-second zone with a 0.5 movement multiplier, followed by a separate 0.5-second visual fade. Leaving a zone removes its slow immediately; overlapping zones retain independent sources and use the strongest slow once, without multiplication. Enemy base speed is unchanged.

Camera rays accept only the configured GameplaySurface collider and reject UI. One SkillDefinition.Radius drives targeting, effects, and placeholder circles. Slow membership uses XZ distance queries and movement/registry notifications, independent of visuals and enemy height.

Both skills share the existing Hand/Elixir commitment pipeline. Empty-area casts still pay and cycle. Cancellation, invalid placement, or insufficient funds never spend Elixir or advance the cycle. CastingTime (before launch) and TravelTime (after launch) are separate delays; no projectile is rendered. The optional ThrowOrigin is captured separately from the target. Pause freezes both delays and area/fade timers; Game Over discards pending casts and freezes the scene until Retry/exit destroys its runtime objects.

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

Use **Defense → Run Prototype Validation (Play Mode)** outside Play Mode. The existing suite includes SkillInteractionValidation for real camera/physics/UI raycasts, cancellation, Elixir and card cycling, AoE boundaries, separate cast/travel delays, and source-aware slow enter/exit, overlap, lifetime, fade, and cleanup. Reports are written to the ignored Documentation/Report directory. Automated pointer-coordinate tests are not manual mouse testing.
