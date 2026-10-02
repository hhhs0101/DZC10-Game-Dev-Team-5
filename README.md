# Unity Defense — 3D Tabletop Stage 1

A Unity **6000.6.0f1** project that extends the existing combat prototype with a session-based Lobby, a six-slot Deck Editor, and a three-card gameplay Hand shared by towers and skills. Final graphics and disk persistence are not implemented.

## Current Migration Status

The Test Stage now uses a fixed perspective camera, a 3D table, Sphere enemies, and Cuboid towers. Gameplay stays planar on **XZ**; height does not affect range, targeting, movement speed, or skill radius.

**Stage 1 intentionally defers 3D mouse input.** Hand selection and menu/HUD controls work, but clicking slots to build and holding/releasing to aim skills are not connected yet. The old 2D world input has been removed to avoid incorrect coordinates. Placement/casting APIs and all cost/cycle rules remain available and tested. Stage 2 will connect them to camera raycasts.

See `Documentation/3D_Stage1_Report.md` for the full status, scene setup, changed files, preview, and verification. The supplied prompt is preserved in `Documentation/3D_Implementation_Stage1.md`.

## Getting Started and Controls

1. Open the `GameDesign` folder in Unity Hub, open `Assets/Defense/Scenes/MainMenu.unity`, and enter Play Mode.
2. Select **Start Game → Lobby**. Navigate with **A / D** or click the left or right stage card. Holding a key repeats navigation every 0.15 seconds after an initial 0.4-second delay.
3. Once the selected stage is centered, press **Enter** or click **Enter Stage**. The list contains stages 1-1 through 1-10; only 1-1 is initially unlocked under the existing progression rules.
4. Open **Deck** and drag a tower or skill card from the left page onto a T1–T6 slot on the right. Changes apply immediately. Select **Back to Lobby** to return.
5. Enter the stage to observe enemies moving from the right side of the tabletop toward the Base on the left. Hand cards can be selected. World mouse placement and skill aiming are deferred to Stage 2; successful API-driven uses still pay costs and rotate the common Hand/Queue.
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

Green placement slots have available, highlighted, and occupied presentation states. Mouse hover wiring is deferred to Stage 2. HP bars appear above the red enemies. All 15 tower types share the existing BasicTurret prefab and combat code. Scroll the left page of the Deck Editor to view additional cards. The complete statistics are listed in `Documentation/TestCatalog.md`.

## Cards, Elixir, and Skills

`CardDefinition` holds a stable ID, display name, placeholder color, and Elixir cost. `TowerCardDefinition` references the existing `TurretDefinition`; `SkillCardDefinition` references a `SkillDefinition`. Combat remains outside the UI.

Every run starts with **3 Elixir**, regenerates **0.36 per second** while Playing, and caps at **10**. Pause and Game Over stop regeneration. The vertical bar on the right smoothly follows the authoritative value. Retry resets it.

| Skill | Damage | Radius | Casting Time | Elixir |
|---|---:|---:|---:|---:|
| Fireball | 60 | 1.5 | 0 s | 3 |
| Arrow Rain | 25 | 3 | 0 s | 2 |

Both retain their data and programmatic casting pipeline; mouse aiming awaits Stage 2. Both use a shared circular damage effect and the existing Enemy damage/reward API. Empty-area casts still consume the card. Cancellation, invalid placement, or insufficient funds never spend Elixir or advance the cycle. Future non-zero casting time is supported: commit, pay and cycle immediately, then apply the effect after the delay. Pausing freezes that delay; Game Over discards pending effects.

Tower Cards currently cost **0 Elixir** and retain their existing resource costs. Their Elixir cost can be changed independently in the card asset. The default deck remains the original six towers; add Fireball and Arrow Rain through the Deck Editor.

New data lives in `Assets/Defense/Data/TowerCard*.asset`, `FireballCard.asset`, `ArrowRainCard.asset`, and the corresponding skill definitions. `Documentation/CardElixirSkillUpdate.md` lists the architecture, files, configuration, and verification details.

## Documentation

- `Documentation/DeckLobbyHandRetryPatch.md`: Duplicate-card root cause and fixes, mouse navigation, Retry, and verification report.
- `Documentation/LobbyDeckHandUpdate.md`: Full change list, data structures, card cycling, and implementation scope.
- `Documentation/Architecture.md`: Current architecture.
- `Documentation/SceneSetup.md`: Scene and Inspector configuration.
- `Documentation/Validation.md`: Current automated verification and unverified behavior.
- `Documentation/Main.md`, `Tower.md`, `enemy.md`, `Lobby.md`, `Deck.md`, and `HandCycle.md`: Copies of the original specifications.

The project uses Unity's built-in uGUI and modules. No additional external packages have been added. Keep `.meta` files alongside their corresponding assets.
