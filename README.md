# Food Defense

A Unity **6000.6.0f1** single-player, 2D tower defense game set in a pixel-art kitchen. Build condiment towers on wooden plates, defend against vegetable enemies, and clear stages to unlock more stages and cards.

This README describes the current campaign and runtime implementation. Older specifications in `Documentation/Prompt/` record development requests; their original numbers and feature lists may differ from the current game.

## Open and play

1. Clone this repository and add its root folder to Unity Hub.
2. Open it with **Unity 6000.6.0f1** and allow Unity to import assets and restore the packages in `Packages/manifest.json`.
3. Open [MainMenu.unity](Assets/Defense/Scenes/MainMenu.unity), then press Play and select the **Game** tab.
4. Choose **Start Game**, select an unlocked stage in the Lobby, and choose **Enter Stage**. Use **Deck** to edit that stage's presets.

Start from MainMenu to exercise the complete navigation and progression flow. Playing a battlefield scene directly uses its serialized stage configuration unless an active session overrides it.

The menu uses a 1280 × 720 reference layout. The battlefield uses an orthographic camera; resizing the Game view does not change the authored world coordinates. The supported gameplay mode is **2D only**.

Do not run prototype rebuild/setup commands just to open the game. These are authoring tools and can replace configured scenes, data, or Inspector adjustments.

## Game flow and controls

```text
Opening video → Start screen → Story (first time) → Lobby
                                                   ├─ Stage carousel → Enter Stage → Gameplay
                                                   ├─ Deck Editor / Presets
                                                   ├─ How to Play
                                                   └─ Settings
Gameplay → Pause / Resume / Settings / Exit to Lobby
         → Victory → Replay / Next Stage / Lobby
         → Game Over → Retry / Lobby
```

The opening video is optional when its resource is missing and remains independent of the saved story/tutorial flags. It can be skipped with a key or click. Story and tutorial completion or skipping is remembered; **How to Play** remains available for manual replay.

| Context | Input / behavior |
|---|---|
| Lobby | **A / D** or click side stage cards to move the carousel; **Enter** or **Enter Stage** to play the selected unlocked stage. |
| Deck Editor | Drag an available card onto a deck slot; select a preset to switch decks. Card clicks show information. |
| Tower card | Select a card, then click an empty wooden plate; dragging a Tower Card onto a valid plate is also supported. |
| Selected card | Click the same card again to deselect it, or another card to switch selection. |
| Tower inspection | With no card selected, click a placed tower to display its attack range. |
| Skill card | Select it, hold the mouse on the battlefield to aim, then release on a valid area to cast. Releasing outside the valid area cancels. |
| Pause | Click **Pause** or press **Esc**. Resume continues the same run; Exit returns directly to the Lobby without another confirmation. |
| Boss introduction | **Season it!**, **Space**, or **Enter** continues after the boss information screen. |

Settings provide BGM and SFX volume/mute controls. Changes save immediately. **Developer Mode** bypasses stage locks for the current session; it resets on restart and does not remove card-pool restrictions. Resolution settings are not implemented.

## Campaign stages

The [GameCatalog](Assets/Defense/Resources/GameCatalog.asset) exposes three campaign stages. Stage 1 is initially accessible; clearing a stage records completion and unlocks the next stage.

| Display name | Stable save ID | StageDefinition | Gameplay scene | Spawn plan | Scheduled entries | Initial deck size |
|---|---|---|---|---|---:|---:|
| Stage 1 | `stage_1` | [Stage1_1.asset](Assets/Defense/Data/Stage1_1.asset) | `TopDownStage3` | [Stage1.json](Assets/Defense/Data/SpawnPlans/Stage1.json) | 86 | 3 |
| Stage 2 | `stage_2` | [Stage1_2.asset](Assets/Defense/Data/Stage1_2.asset) | `TopDownStage1_2` | [Stage2.json](Assets/Defense/Data/SpawnPlans/Stage2.json) | 98 | 6 |
| Stage 3 | `stage_3` | [Stage1_3.asset](Assets/Defense/Data/Stage1_3.asset) | `TopDownStage3` | [Stage3.json](Assets/Defense/Data/SpawnPlans/Stage3.json) | 97 | 6 |

Entry counts describe the authored JSON lists, including scheduled bosses. Summoned enemies and split descendants can increase the number encountered. All three definitions currently start with **1000 Base HP** and **300 Resources**.

`MainMenu`, `TopDownStage3`, and `TopDownStage1_2` are enabled in [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset). Other test definitions and development scenes remain in the project but are not additional selectable campaign stages.

Stage 3 applies its diverging-route configuration when initialized. Scene names are implementation identifiers, so `TopDownStage3` does not mean that only campaign Stage 3 uses that scene.

## Decks, cards, and Energy

- `PlayerDeck` holds **3–6 unique CardDefinition references**, including at least one Tower Card. Each restricted stage has its own three saved presets; its default deck determines the preset size.
- Deck slots are persistent editing positions, **not battle draw order**. Cards already in the current deck are excluded from Available Cards.
- Every stage start copies the selected deck and shuffles that copy once. The first three entries become the Hand; the remaining entries form the Upcoming Queue. There is no reshuffle during that run.
- A successful tower placement or committed skill cast advances the cycle. The used card moves to the queue's end and the next queued card enters the Hand. With a three-card deck, the used card returns to the end of the Hand.
- Failed placement, canceled aiming, and insufficient Energy do not spend Energy or advance the cycle.
- **Energy is an integer: start 2, maximum 10, regenerate 1 every 2.8 seconds of active gameplay.** Pause and other time freezes stop regeneration. Yellow segments fill the bar from bottom to top.

Current stage card pools grow through progression:

| Pool | Towers |
|---|---|
| Stage 1 base pool | Salt, Black Pepper, Paprika Powder |
| Stage 2 additions | Ketchup, Mustard, Olive Oil |
| Stage 3 additions | Mayonnaise, Hot Sauce, Soy Sauce |

Clearing stages expands the available pool through `StageCardRules`; already unlocked cards can be used when revisiting earlier stages. Existing presets are validated against the available pool and stable card IDs when loaded.

The global catalog contains **11 Tower Cards and 2 Skill Cards**. Vinegar and Brown Sauce also exist globally. **Toy Throw** and **Slow Area** are implemented, but neither Skill Card is in the current campaign's authored Allowed Cards arrays. To use them in a configured stage, add their definitions to its permitted card pool and deck through the existing data model.

## Combat

Towers can be placed only on predefined, unoccupied wooden plates. Placement validates the slot and definition before spending the selected card's Energy. Plates highlight on hover, and destroyed or expired towers release their slots.

Tower behavior shares the `Tower` abstraction:

| Tower behavior | Current examples |
|---|---|
| Direct attacks | Vinegar, Black Pepper, Paprika Powder, Salt, Brown Sauce |
| Slowing attacks | Olive Oil |
| Burning attacks | Hot Sauce |
| Long-range attacks | Soy Sauce |
| Shield-piercing attacks | Mustard |
| Splash attacks | Ketchup |
| Support damage buff and periodic disruption cleanse | Mayonnaise |

`AttackTower` owns target selection and attack timing; specialized attack subclasses add effects. `SupportTower` shares tower health/lifetime behavior without implementing direct attacks. Targeting is replaceable through the targeting strategy abstraction. Exact statistics and Energy costs belong to the definition/card assets, not UI callbacks.

Enemies share health, path movement, status effects, and lifecycle handling. Their definitions select stats, visuals, prefabs, and ability configuration. Implemented behaviors include ally speed support, shield support, tower disruption, Corn cluster targets, summoning, and death splitting.

The current bosses are:

- **Baechu:** a high-health enemy, currently 2000 HP.
- **Gosari:** 2000 HP, splitting on combat death into two 800-HP children; each child splits into two 400-HP grandchildren. Later generations are smaller and faster.
- **Doraji:** 4000 HP with a summoning behavior that produces three distinct enemy types per summon cycle.

Enemy HP bars follow health changes. A defeated enemy leaves active combat immediately; its faded presentation can continue along the route without damaging the Base. A living enemy reaching the endpoint applies its configured Base damage.

A stage clears when its spawn schedule has finished and the registry has no living enemies. Killing every scheduled enemy is not required if the Base survives their arrivals. **Game Over** occurs when Base HP reaches zero. Retry and Replay reload the same stage as a fresh run, including a new shuffle, while retaining saved decks and progression.

Skill aim graphics and hit membership share `SkillRangeProfile` / `EllipseFootprint` in XY space. The outside glow is decorative. Committed delayed casts use their stored impact center and evaluate enemy positions when their effect arrives. Tower range inspection remains circular.

### Economy integration status

Energy pays for current tower/skill card use. The separate Resource wallet is initialized by StageDefinition and receives each defeated enemy's configured Resource reward through `EnemyKillRewards`.

Gold/Candy persistence and reward helpers remain in the codebase, but the current clear controller sets Gold earned to zero and does not award `ClearGoldReward`. The campaign scenes/initializer do not initialize a `CandyDropSystem`. Those stored fields and helper classes should not be taken as evidence of active campaign Gold or Candy rewards.

## Edit stage spawn schedules

StageDefinition retains scene routing, Base HP, card rules, and other stage settings. Its **Spawn Plan Json** TextAsset controls enemy order and relative delays through the existing spawner.

```json
{
  "stageId": "example_stage",
  "spawns": [
    { "enemyId": "enemy_carrot", "delay": 1.0 },
    { "enemyId": "enemy_tomato", "delay": 2.0 },
    { "enemyId": "enemy_potato", "delay": 0.5 }
  ]
}
```

`delay` is the wait before that entry, relative to the preceding spawn; the first entry waits from stage start. Repeat entries to spawn the same enemy repeatedly. Zero delay allows consecutive entries in the same update. The plan loads once per run and pauses with gameplay.

`EnemyDefinitionCatalog` resolves readable enemy IDs to existing EnemyDefinitions. Optional `routeGroup` values are supported for routed stages. The JSON `stageId` is descriptive; stage progression uses the stable ID on StageDefinition.

Precedence is explicit:

1. Assigned JSON plan: exclusively controls the schedule/count.
2. No JSON: use the legacy Spawn Sequence and fixed interval when populated.
3. Neither: use the legacy single-enemy interval/count configuration.

Malformed assigned JSON, unknown IDs, and invalid delays produce a configuration error rather than silently falling back. Changing legacy Total Enemies does not change a JSON-backed stage's count. There is no full wave editor, repeat directive, or runtime JSON hot reload.

## Project structure and responsibilities

```text
Assets/Defense/
  Scenes/           Main menu and native 2D battlefields
  Data/             Stage, enemy, tower, card, and skill definitions
    SpawnPlans/     Authored stage JSON schedules
  Resources/        GameCatalog, enemy lookup, UI/audio resources
  Prefabs/          Enemy, tower, and placement prefabs
  Art/              Battlefield, UI, enemy, and tower artwork
  Runtime/
    Core/           Session, initialization, GameFlow, Energy, economy, saves
    Data/           ScriptableObject definition types
    Lobby/          Stage selection and persistent progression
    Deck/           Deck validation, presets, card pools, persistence
    Hand/           Per-run cards, shuffle/cycle, selection
    Placement/      Slot validation and tower placement
    Turrets/        Shared tower hierarchy, targeting, combat, lifetime
    Enemies/        Enemy lifecycle, movement, spawning, special behaviors
    Spawning/       JSON parsing and resolved spawn-plan data
    Skills/         Casting, elliptical membership, effects and markers
    World2D/        Battlefield pointers, sprite views, camera and inspection
    UI/             uGUI menus, HUD, dragging, card and result presentation
    Audio/          Audio catalog, playback, saved volume controls
  Editor/           Explicit authoring tools and custom validation harnesses
Documentation/
  Prompt/           Tracked development prompts/specifications
  Report/           Local implementation reports and validation output (ignored)
SaveData/           Local progression/onboarding history (ignored)
```

[StageInitializer](Assets/Defense/Runtime/Core/StageInitializer.cs) wires each scene's components once. [GameFlow](Assets/Defense/Runtime/Core/GameFlow.cs) owns Playing, Paused, Victory, GameOver, and ConfigurationError. [PlayerSession](Assets/Defense/Runtime/Core/PlayerSession.cs) owns the cross-scene selection, deck managers, progression, and currency services.

UI selects cards or invokes controllers; placement, combat, and spawning remain separate. Definitions hold configuration, while runtime objects hold health, cooldowns, modifiers, and per-run state. Components communicate through explicit initialization and events such as health changes, deaths, selection changes, and progression updates.

## Saved data

| Data | Location |
|---|---|
| Stage completion and unlocks | `SaveData/stage-progress.json` |
| Story/tutorial seen flags | `SaveData/onboarding-progress.json` |
| Deck presets | `Application.persistentDataPath/deck-presets-Stage1_1.json`, `deck-presets-Stage1_2.json`, and `deck-presets-Stage1_3.json` |
| Legacy unrestricted deck fallback | `Application.persistentDataPath/deck-presets.json` |
| Gold/Candy balances | `Application.persistentDataPath/player-currency.json` |
| BGM/SFX volume, including mute as zero volume | Unity PlayerPrefs: `Defense.Audio.BgmVolume`, `Defense.Audio.SfxVolume` |

In the **Editor**, `SaveData` is created inside the project root. In desktop builds it is beside the executable, or beside the `.app` bundle on macOS. The installation directory must be writable. Deck/currency files still use Unity's platform-specific persistent data directory; they have not moved into `SaveData`.

Stage clears save stable completed/unlocked stage IDs automatically. Renaming a display label is safe; changing a released StageDefinition's stable ID changes its saved identity. A missing project-local progression file can import the old persistent-data file once; the new file then takes precedence. Onboarding can likewise import older PlayerPrefs flags when its JSON is missing.

For story/tutorial retesting, stop the game, set `storySeen` and/or `tutorialSeen` to `false` in `SaveData/onboarding-progress.json`, then restart. This does not reset the opening video. Back up saves before resetting progression; deleting its new file may import an existing legacy save again.

`SaveData/` is Git-ignored, so cloning the repository does not share another player's history. Runtime-generated player saves are not game configuration files and should not be committed.

## Third-party artwork

Read [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and the asset pack's original terms before redistributing artwork. Helm3t pack imports belong in the ignored `Assets/Art/ThirdParty/Helm3tsVeggies/` folder. Do not force-add the pack or its source archive.

To restore that pack's locally imported sprites:

1. Obtain the [Fruit and Veggies pack from Helm3t](https://helm3t.itch.io/free-32x32-fruits-and-veggies-asset-pack) separately and review its current license.
2. Extract it, then in Unity choose **Defense > Enemies > Import Veggies from Folder...**.
3. Select the extracted `VEGGIES/VEGGIES_NL` folder. The tool copies the required sprites and reconnects definitions.
4. For the additional role-art mapping, use **Defense > Enemies > Reimport Role Artwork from Folder...** when needed.

Some current enemy artwork is also tracked under `Assets/Defense/Art/Vegetables/`. This is distinct from the ignored pack import location; its presence in Git does not establish redistribution rights. Check provenance and permissions before distributing source assets. Missing optional local sprites can use placeholder presentation.

## Verification and development limits

The project contains custom Editor validation harnesses rather than a configured NUnit / Unity Test Framework suite. **Defense > Run Prototype Validation (Play Mode)** starts the general harness. Focused harnesses in [Assets/Defense/Editor](Assets/Defense/Editor) cover progression persistence, deck persistence, stage card rules, JSON spawning, selection toggling, elliptical skills, tower hierarchy, Corn combat, and Gosari splitting.

These fixtures can enter Play Mode and switch scenes; save your scene edits first. Persistence tests should use isolated test paths rather than real player saves. Runtime loaders support the `DEFENSE_TEST_DECK_SAVE_PATH`, `DEFENSE_TEST_CURRENCY_SAVE_PATH`, `DEFENSE_TEST_PROGRESS_SAVE_PATH`, and `DEFENSE_TEST_ONBOARDING_SAVE_PATH` environment overrides in the Editor.

For a quick gameplay check, start MainMenu, enter an unlocked stage, place a tower, pause/resume, and exercise Retry/Replay and return-to-Lobby. Restart to check saved presets, stage progress, audio volume, and onboarding flags. Verify these interactions in the actual Editor/player; static inspection alone does not prove them.

Remaining limits include incomplete currency-reward integration, no resolution controls or cloud saves, and mixed development/test assets outside the active campaign. Full prototype rebuild tools and older validation fixtures may encode historical defaults; inspect them before applying them to authored content. Build/platform behavior and third-party asset rights require their own verification.
