# Unity Defense — 2D Top-Down Prototype

A Unity **6000.6.0f1** project that extends the existing combat prototype with a session-based Lobby, a six-slot Deck Editor, and a three-card gameplay Hand shared by towers and skills. Decks persist to disk as three independent presets; Gold and Candy also persist. Final graphics and progression saves are not implemented.

## Elliptical Skill Range and Glow Marker

Toy Throw and Slow Area use the supplied transparent glow Sprite and a shared `SkillRangeProfile` at `Assets/Defense/Data/SkillRangeProfile.asset`. Their existing Radius is the horizontal semi-axis; the vertical semi-axis is Radius × 400/1518 (approximately 0.2635046). The two current radii remain 1.5 and 3, producing bright-rim diameters of approximately 3 × 0.7905 and 6 × 1.5810 world units. Damage, costs, durations, stable IDs and save formats are unchanged.

The original texture is 1774 × 887. Its alpha ≥128 bright-rim calibration is `(128,176,1518,400)` in bottom-left texture coordinates. Uniform default Sprite scaling uses that rim, not the full texture. `SkillAreaVisual` offsets only the artwork from its imported center pivot to align the rim center with the exact gameplay target. The outside glow is decorative; transparent interior points still count as inside.

`EllipseFootprint.Contains` supplies the final test for Toy Throw and persistent slow membership, using Enemy root positions in XY and ignoring Sprite sorting depth. The existing EnemyRegistry is the candidate collection; multiple colliders cannot cause duplicate hits. Delayed casts retain their committed impact center and test current Enemy positions on arrival. Slows keep independent sources and remove only their own modifier on exit, expiry or destruction. Terminal game states immediately hide transient glow and remove slow sources; Pause freezes lifetime/fade as before.

The shared visual component's circle API remains dedicated to Tower attack-range inspection. Only Skills use the Sprite path; their old circle renderer stays disabled. Each Sprite presentation has an owner-managed world transform so inherited nonuniform scale/rotation cannot distort it independently from gameplay geometry. Scene teardown and owner destruction remove the marker.

Fine-tune the aspect and measured rim in the shared profile; visual scaling and collision use the same settings. Future area SkillDefinitions must reference a range profile. No Sprite Editor package, runtime file loading, new collider or external rendering package is required. `SkillEllipseValidation.Run` covers geometry, calibrated rendering, transformed parents, actual damage/slow behavior, delayed impacts and cleanup. Native regression tests also retain card cancellation, Hand/Elixir and the circular Tower inspection checks.

## Enemy Roles, Abilities and Specified Balance

The existing shared `Enemy` / `BasicEnemy` actor, native XY path, health, Sprite observer, rewards and spawner remain in use. `EnemyDefinition.Role` selects composed abilities; it does not change persistent enemy IDs or require per-vegetable prefabs. Existing Tomato and Potato assets/GUIDs are reused. The other original eight vegetable variants retain their previous statistics.

| Enemy | Role | HP | Base speed | Endpoint damage |
|---|---|---:|---:|---:|
| Carrot | Basic | 100 | 2.0 | 100 |
| Tomato | Basic | 85 | 3.0 | 100 |
| Eggplant | Basic | 150 | 1.7 | 100 |
| Garlic | Speed Support | 30 | 2.0 | 20 |
| Broccoli | Shield Support | 200 | 1.8 | 30 |
| Potato | Disruptor | 150 | 1.8 | 20 |
| Boss | Boss | 2500 | 1.2 | **Not configured** |

Garlic uses a 1.2× speed multiplier on other living allies. Independent Garlic sources use the strongest boost; effective speed is base speed × strongest boost × strongest existing slow. Membership follows XY movement notifications, and leaving, disabling or removing a source removes only that source. Broccoli has one finite shared 100-HP pool per spawn/reset. It excludes itself, absorbs incoming damage before ally HP, and passes any remainder to that ally. Multiple shields process the remainder once each in activation order. Pool depletion, reenable and moving allies never replenish a shield. Inspect ShieldSupportAbility's Remaining Shield field during Play Mode for minimal debug feedback.

Potato selects the nearest active, initialized, actually placed Tower in XY range. It applies a target-owned timed attack-disable token; cards, placement, visuals, range inspection and Tower stats are unchanged. The normal attack cooldown continues while disabled. The first ability attempt occurs on its first positive-time update, then attempts (including misses) follow its configured cooldown. Reapplying from the same source refreshes that source; separate sources expire independently. Applied disruption survives Potato death until expiry, while its token holds no reference back to the source. Pause freezes timers and attempts.

**Required design tuning remains TBD:** Garlic/Broccoli Support Area radii and Potato Disruption Range, Cooldown and Duration have no supplied values. Zero is an explicit unconfigured sentinel, not a chosen balance value. These abilities warn at spawn and stay inactive until every required field is positive and finite. The current support balance is 1.2× and 100 HP, updated by the user after the original specification. Boss endpoint damage remains explicitly unconfigured (`Base Damage Unconfigured`); reaching Base retires it safely without applying invented damage or kill rewards. No Tower HP system or special Boss attack was added. Radish remains a documented alternative and has no playable definition or ability.

The original five-enemy repeatable Test Stage, all authored spawn lists, path/slots and Base HP settings are unchanged. Test Stage 2 still references the same Tomato/Potato definitions, so it now uses their specified higher stats/damage; its existing 10 Base HP can be depleted by a single new Tomato arrival. New role definitions are not silently inserted into release stages.

Correct Carrot, Eggplant, Garlic and Broccoli PNGs were found in the original licensed pack and imported locally with the existing Point/Single/32 PPU convention. Tomato/Potato keep their existing art. Boss identity/art is missing and uses the shared actor's clearly temporary placeholder in the local test harness. All pack PNGs/metas remain in Git-ignored `Assets/Art/ThirdParty/Helm3tsVeggies`. On another checkout, obtain the pack separately; the existing import menu restores the original ten variants, and **Defense > Enemies > Reimport Role Artwork from Folder...** binds the four new matching images without resetting statistics. Existing third-party notices apply.

**Defense > Enemies > Start Role Sandbox (Test Tuning)** opens the existing local Stage2 fixture and creates seven runtime-only definition copies with explicit test settings: support radius 3, Potato range 6, cooldown 2 seconds and duration 1.5 seconds. These are test values, not approved balance. Enemies are stopped at points reached along the real path for Inspector/placement checks; use the existing Hand to place Towers. The fixture has 10000 test Base HP/resources and full Elixir; it never initializes PlayerSession persistence. Stop Play Mode to discard it. No scene or definition asset is saved by the harness. `EnemyRolesSetup.ApplyDefaults()` is an explicit patch authoring command, not a startup hook; it reapplies the supplied stats and preserves configured ability tuning/rewards/IDs.

`EnemyRolesValidation.Run()` combines custom EditMode bookkeeping checks with PlayMode integration. The project has no NUnit/Test Framework package, so no extra package was added. Current verification results and the known legacy full-suite camera assertion are documented in ignored `Documentation/Report/Enemy_Roles_Abilities/Implementation_Report.md`.

## Condiment Tower Artwork and Names

The existing fifteen playable Towers and their cards retain all stable IDs and asset filenames. Towers 1–11 now use the following data-driven names and artwork. Towers 12–15 keep their existing numbered names and placeholders; no cards or combat types were added.

| Tower | Display name | Supplied PNG | Imported Sprite |
|---|---|---|---|
| 1 | Vinegar | `Pixel Art Vinegar Bottle128nb.png` | `Assets/Defense/Art/Condiments/Vinegar.png` |
| 2 | Olive Oil | `Pixel-Art Golden Oil Bottle128nb.png` | `Assets/Defense/Art/Condiments/OliveOil.png` |
| 3 | Hot Sauce | `Pixel-Art Hot Sauce Bottle128nb.png` | `Assets/Defense/Art/Condiments/HotSauce.png` |
| 4 | Black Pepper | `Pixel-Art Pepper Shaker128nb.png` | `Assets/Defense/Art/Condiments/BlackPepper.png` |
| 5 | Paprika Powder | `Pixel-Art Red Spice Jar Icon128nb.png` | `Assets/Defense/Art/Condiments/PaprikaPowder.png` |
| 6 | Salt | `Pixel-Art Salt Shaker Sprite128nb.png` | `Assets/Defense/Art/Condiments/Salt.png` |
| 7 | Brown Sauce | `Pixel-Art Sauce Bottle Icon128nb.png` | `Assets/Defense/Art/Condiments/BrownSauce.png` |
| 8 | Soy Sauce | `Pixel-Art Soy Sauce Bottle.png` | `Assets/Defense/Art/Condiments/SoySauce.png` |
| 9 | Mustard | `Pixel-Art Yellow Mustard Bottle (1)128nb.png` | `Assets/Defense/Art/Condiments/Mustard.png` |
| 10 | Mayonnaise | `Retro Pixel Art Mayonnaise Bottle128nb.png` | `Assets/Defense/Art/Condiments/Mayonnaise.png` |
| 11 | Ketchup | `Retro Pixel-Art Ketchup Bottle128nb.png` | `Assets/Defense/Art/Condiments/Ketchup.png` |

The supplied Soy Sauce file is `Pixel-Art Soy Sauce Bottle.png`; the prompt's `Pixel-Art Soy Sauce Bottle(1).png` was absent. It is the sole matching Soy Sauce image in the supplied folder. All eleven supplied images are assigned.

Each TurretDefinition exposes optional Sprite, Sprite Scale and Sprite Offset fields. The shared BasicTurret2D prefab's TurretSpriteView observes initialization and updates only its Visual child. TowerCardDefinition.Icon resolves that same Sprite for the Deck Editor, Lobby deck preview and gameplay Hand; Skill cards keep their existing presentation. No runtime code reads Downloads or chooses behavior by Tower name/number.

PNGs are copied byte-for-byte into Assets, imported as Single/Point/uncompressed Sprites at 128 PPU, with alpha, no mipmaps, no NPOT resizing and centered pivots. The non-negligible alpha silhouette (alpha >= 16) determines a uniform 1.35-unit visible height and presentation offset so the visible bottom center meets the unchanged placement point. Very faint export halos are retained in the PNG and ignored only for alignment. Sorting order remains 40, above the plate's 20; root transforms, slot colliders and ProjectileOrigin are unchanged.

`CondimentTowerSetup.Apply(sourceFolder)` is an explicit Editor authoring operation for restoring the exact mapping; it does not run automatically. Reusing the original scratch prototype builder can reset authored placeholder data, so preserve custom Inspector edits before invoking full rebuild tools. Actual sprite references are ordinary project assets. Saved presets still resolve `tower.test.1` through `tower.test.15`; they are not migrated or regenerated from display names. Local verification and screenshots are recorded in ignored `Documentation/Report/Tower_Condiment_Migration/`.

## Wooden Plate Placement Slots

The common `Assets/Defense/Prefabs/TopDown2D/PlacementSlot2D.prefab` uses `Assets/Defense/Art/TopDown2D/WoodenPlate.png` on its existing **SlotVisual** SpriteRenderer. The supplied 154×102 RGBA image is imported unchanged as a Point-filtered Single Sprite at 154 PPU, so its unscaled canvas is 1×0.662 world units. SlotVisual now uses a uniform 1.5× scale in X and Y, displaying approximately 1.5×0.994 world units while preserving its proportions. Its centered 1×1 BoxCollider2D and independent PlacementPoint remain unchanged.

Available plates keep their original wood colors, hovered plates receive a warm gold tint, and occupied plates retain their original wood colors. Existing `PlacementSlotSpriteView` observes the same slot state; no placement rules were changed. Sorting remains 20, below Towers at 40. Change the SpriteRenderer's Sprite or the view's serialized colors in the prefab Inspector for future art replacement; keep gameplay collider/root scale separate. Local verification details are recorded in ignored `Documentation/Report/Wooden_Plate_Slot_Report.md`.

## 2D Kitchen Map

The default native 2D battlefield (`TopDownStage3`) now displays the supplied kitchen interior and transparent gingham track. Enemies spawn near the left refrigerator, follow the complete cloth route through both lower bends and the middle rise, and reach the upper-right Base. Stage content, the five-enemy Test Stage, Test Stage 2's ten-entry list and all combat/reward/persistence rules are unchanged.

Original PNG copies are in `Assets/Defense/Art/Kitchen2D`. Both are 1672×941, imported as Single/Point Sprites at 80 pixels per unit, with original alpha, no mipmaps and no compression. Background order is 0; cloth order is 10. The current route uses the supplied larger replacement PNG. Background and route are both unscaled at (0,.5,0), aligning their original canvases; the cloth is already drawn in the floor region. The movement route has 48 editable waypoint references, and only three formerly overlapping slots were moved clear of the new fabric. Neither source artwork nor its aspect ratio was edited.

In the scene, edit `MapRoot/KitchenBackgroundSprite`, `MapRoot/GinghamPathSprite`, `MapRoot/WaypointPath2D`, `EnemySpawnRoot (Refrigerator)`, `MapRoot/BaseVisualAnchor`, and `MapRoot/PlacementSlots2D`. The spawn and Base Transforms are also the first and last waypoint references, keeping their positions synchronized. Eight independent slots sit off the visible cloth. The floor's BoxCollider2D defines the permitted interaction area; kitchen furniture remains decorative. The previous placeholder tabletop and path debug line are retained but inactive. Enable `PathVisualization` to inspect the actual movement path.

The existing orthographic camera frame now optionally uses a SpriteRenderer's bounds, allowing it to show the entire kitchen while the playable floor has its own smaller bounds. The default minimum half-height is 8.5; HUD/Hand/Elixir remain screen-space. **Defense > Maps > Apply Kitchen 2D Layout** explicitly reapplies the initial scene layout and replaces waypoint/slot positions; normal Play Mode never does so. Save or back up Inspector adjustments before using that authoring command. See local ignored `Documentation/Report/Kitchen_2D_Map_Report.md` for actual verification and remaining limits.

## JSON Stage Spawn Plans

Edit [Assets/Defense/Data/SpawnPlans/TestStage2.json](Assets/Defense/Data/SpawnPlans/TestStage2.json) to control Test Stage 2's ordered spawn schedule. Its StageDefinition already references this TextAsset. The sample retains the original ten vegetable types/order and one-second cadence; the legacy fields remain stored unchanged. No YAML GUID editing is needed for future order/delay changes.

```json
{
  "stageId": "test_stage_2",
  "spawns": [
    { "enemyId": "enemy_mushroom", "delay": 1.0 },
    { "enemyId": "enemy_tomato", "delay": 2.0 },
    { "enemyId": "enemy_potato", "delay": 0.5 }
  ]
}
```

The shorter example above produces Mushroom at 1.0 gameplay seconds, Tomato at 3.0 and Potato at 3.5. Delay means time since the previous successful spawn; the first delay starts at spawn-phase entry. Repeated IDs create repeated enemies. Zero is valid and consecutive zero-delay entries can spawn in the same frame, in order. Positive delays restart after the actual preceding spawn, without borrowing frame overshoot; ordinary frame tolerance therefore applies. Pause freezes elapsed gameplay time. Plans are read once per run; edit the file and restart the stage to apply changes. There is no hot reload, external built-game file replacement, wave/count/repeat syntax or random scheduling.

Stage initialization chooses exactly one source: assigned Spawn Plan Json exclusively controls the schedule; otherwise a nonempty Spawn Sequence retains the old fixed-interval sequence; otherwise Enemy/Spawn Interval/Finite Spawning/Total Enemies retain their previous finite or infinite behavior. JSON always schedules exactly its list length, independently of legacy count/finite fields. Runtime PlannedSpawnCount/IsFinishedSpawning on FixedIntervalSpawner describe the effective schedule; StageDefinition's old fields/properties retain their legacy meaning. Finishing spawning still requires no living enemies and a surviving Base before Stage Clear.

[Assets/Defense/Resources/EnemyDefinitionCatalog.asset](Assets/Defense/Resources/EnemyDefinitionCatalog.asset) explicitly references fifteen real definitions with existing stable IDs. It is loaded at one known Resources path in player/runtime code; no AssetDatabase search is used at runtime. It is separate from EnemyRegistry, which still owns living enemies. IDs are case-sensitive EnemyDefinition.EnemyTypeId values such as enemy_mushroom, enemy_tomato and enemy_potato, independent of display names/art. The old BasicEnemy/RewardTestEnemy assets have no logical IDs and remain available through legacy scheduling; assign unique stable IDs and add their definition references to the catalog if future JSON plans need them. Existing IDs are not changed by this patch.

The strict small-schema loader validates the entire JSON and catalog before the first spawn. It requires a nonblank diagnostic stageId, a nonempty spawns array, and explicit finite nonnegative numeric delays and resolvable enemyId strings. Missing/negative/string/null/non-finite delays, duplicate/unknown fields, empty plans and duplicate/missing catalog IDs or references produce an error. stageId is diagnostic only: this project has no existing stable stage ID, so it does not select/load scenes or compare against displayName.

Invalid assigned JSON never falls back to legacy spawning. GameFlow enters a distinct terminal ConfigurationError state, freezes gameplay, blocks Card/world input and shows a Stage Configuration Error overlay with details, Retry and Return to Lobby. It does not enter Victory/Game Over or award currency/progression. Fix/reimport the JSON before Retry; an unchanged invalid plan shows the error again. The integrated 2D GameplayUI displays this error panel. JsonSpawnSetup.Run explicitly rebuilds the ID catalog and attaches the sample if restoring authoring dependencies; no manual setup is needed for the committed-style project assets.

JsonSpawnValidation.Run combines strict parsing/catalog checks with actual PlayMode timeline, Pause, file-edit snapshot behavior, error UI, Retry, finite clear and rewards. See ignored Documentation/Report/JSON_Spawn_System/Implementation_Report.md for actual results and the existing legacy full-suite camera assertion.

## Test Stage 2 — Vegetable roster

Select **Test Stage 2** in the Lobby carousel and choose **Enter Stage**. It is always unlocked and uses the existing native 2D battlefield. Exactly one Mushroom, Olive, Green Cabbage, Zucchini, Tomato, Kohlrabi, Turnip, Leek, Red Cabbage and Potato spawn in that order, at one-second intervals. Starting Base HP is 10 and resources are 300; clearing grants the inherited 50 Gold, while each vegetable retains its own existing Candy settings. Replay/Retry starts the same ten-entry list from the beginning. The original five-enemy Test Stage remains unchanged.

`Assets/Defense/Data/TestStage2.asset` now uses its linked **Spawn Plan Json** TextAsset. Its original **Spawn Sequence** Inspector list remains unchanged as a backward-compatible fallback when that JSON reference is cleared. A nonempty legacy list is finite and defines the total count; an empty list retains the existing single-enemy finite/continuous behavior. Each run snapshots only the selected source. No wave framework was added. **Defense > Enemies > Create Test Stage 2** can recreate the initial asset/catalog entry if missing; it preserves existing configured stage data when rerun. Vegetable artwork requires the local pack import described below.

## Veggie Enemy Variants

Ten independently editable definitions are available under `Assets/Defense/Data/Enemies/Veggies`: Mushroom, Olive, Green Cabbage, Zucchini, Tomato, Kohlrabi, Turnip, Leek, Red Cabbage and Potato. Assign one to an existing StageDefinition's **Enemy** field, or pass it to the existing EnemySpawner/FixedIntervalSpawner configuration. The original five-enemy Test Stage and campaign configurations are unchanged.

All ten reuse **BasicEnemy2D**, Enemy/EnemyHealth/EnemyPathFollower and existing Tower/Skill/death/reward handling. EnemyDefinition now provides stable EnemyTypeId, DisplayName and optional Sprite data. The optional EnemySpriteView observes initialization and replaces only the Visual child's Sprite/tint; a definition without Sprite keeps the original placeholder. All initial combat/resource/Candy values copy BasicEnemy: HP 40, speed 1.5, Base damage 1, resource reward 10, Candy chance 10%, amount 1. Edit each definition independently for later balancing.

Artwork is from **Helmet / Helm3t's 32x32 Fruit and Veggie Pack, free full edition (legacy)**. The supplied license allows game use and prohibits raw-asset redistribution. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for creator links, the terms checked, and the repository distribution policy. Individual PNGs and the original license are imported locally under the existing Git-ignored `Assets/Art/ThirdParty/Helm3tsVeggies` directory. Obtain the pack separately on another checkout; then use **Defense → Enemies → Import Veggies from Folder...**, select its `VEGGIES/VEGGIES_NL` folder, and the importer reconnects existing definition Sprite references. The Downloads shortcut uses the supplied pack folder name. Reimport preserves existing gameplay stats and enemy IDs rather than resetting them. Do not commit/publish the ignored raw artwork without separate permission from its author.

Imports preserve the original 32×32 RGBA files: Single Sprite, Point filtering, 32 pixels/unit, Full Rect centered canvas, mipmaps off and no compression. The shared visual canvas is 1.2 units wide/high (twice its original .6-unit size); only the Visual child is enlarged. The .3-radius CircleCollider2D, root scale, HP bar, and combat radii remain unchanged; transparent margins do not redefine hitboxes. Custom artwork is white-tinted with Sprite sorting order 40; existing HP bars remain above it at 60.

To inspect all ten without modifying a Stage asset or player save, use **Defense → Enemies → Open Veggie Sandbox** in Edit Mode. It opens the existing Stage 2 local sandbox and starts Play Mode with ten stationary real enemies in two rows; their Hierarchy names identify them. Its automatic diagnostic schedule is disabled for that Play session. Existing local Tower/Skill cards can interact with the enemies. Stop Play Mode to reset; this preview is not the five-enemy reward Test Stage.

`Defense.Editor.VeggieEnemyValidation.Run` runs focused Play Mode checks with isolated temporary currency. The English implementation report and source/runtime previews are stored locally in ignored `Documentation/Report/`.

## Supported 2D Game Flow

**Main Menu → Start Game → Lobby → Enter Stage** launches the selected StageDefinition's configured 2D scene. Enter / Keypad Enter uses the same action. The five current stage definitions explicitly reference `Assets/Defense/Scenes/TopDownStage3.unity`, the existing integrated kitchen battlefield they already shared. There is one supported gameplay mode and one stage-entry button. Missing scene configuration produces a clear error and never substitutes a different map.

Build Settings contain MainMenu first and TopDownStage3 second. Retry/Replay reloads the same configured scene and stage identity. Returning to Lobby preserves the selected stage, active Deck preset and currencies. Opening the integrated scene directly uses its authored repeatable Test Stage configuration. The separate Stage 1 and Stage 2 scenes remain 2D development diagnostics, outside the build.

The existing kitchen layout, camera composition, sprites, wooden plates, enemy route, shared combat/Hand/Skill services and save formats are preserved. Finite stages clear when spawning finishes and no living enemies remain while the run is still Playing. Base depletion causes Game Over; nonlethal arrivals can still end in a clear. Base arrivals grant no combat resources or Candy. Repeat clears pay the configured Gold and combat Candy persists even after failure.

Historical implementation prompts remain in `Documentation/Prompt/`; they describe earlier development states, not supported runtime routes.

## Native 2D Migration — Stage 2 Interaction Sandbox

Open `Assets/Defense/Scenes/TopDownStage2.unity` and enter Play Mode. Select a Tower Card and click an available slot. Select a Skill Card, press/hold on the tabletop to aim and release to cast; release over UI or off-table to cancel. Pause/Esc use the existing GameFlow. Stop Play Mode to reset this local sandbox; full result/navigation/Retry integration is available separately in Stage 3.

This scene reuses actual BasicEnemy/BasicTurret, Hand, RuntimeCardCycle, Elixir, combat resources, direct-damage attacks and both Skill effects. `TopDownInteractionSandbox` uses an explicit temporary six-card test deck (four existing Tower Cards and both Skill Cards), reading Stage 1-1's unchanged starting HP/resources, Enemy and interval. It does not initialize PlayerSession, use/edit the persistent presets, grant Gold/Candy, or connect Victory/Lobby routing. The playable game uses the integrated 2D scene.

`BattlefieldPointer2D` is the common camera-plane conversion and Physics2D slot-query boundary. It respects the logical Collider2D area, explicit slot layer, child colliders and uGUI interception. Gameplay rules stay in the existing placement/Hand components. Actor prefab overrides supply the two sprite prefabs while original definitions still supply cost and all combat stats. Do not clear those override references when editing the sandbox.

`EnemyRegistry.Plane` is configured to XY for every retained battlefield. The common PlanarSpace helpers, targeting interface and SkillCastContext propagate that plane to distance queries, aim visuals, AoE and source-owned slow areas. No global plane switch or duplicate combat hierarchy exists. HP bars reuse the original health observer with a configurable sorting order. Slow zones use authoritative geometric membership and registry/movement notifications, preserving overlap, exit, lifetime and fade rules.

Focused validation: `Defense.Editor.TopDownStage2Validation.Run` in isolated Unity batch Play Mode, with `-noaudio`, verifies real Hand UI callbacks, screen-coordinate placement/Skill input, Physics2D/uGUI queries and combat over advancing frames. The live Editor can report different Game-view camera and Screen dimensions from Editor callbacks on this macOS setup; isolated batch Play Mode is the reproducible interaction-test environment. Manual input testing is separate.

## Native 2D Migration — Stage 1 Prototype

Stage 1 is available separately in `Assets/Defense/Scenes/TopDownStage1.unity`. Open it directly and enter Play Mode to see one diagnostic red Enemy sprite follow an editable right-to-left XY route. This scene intentionally has no card input, combat, stage rewards or PlayerSession bootstrap yet. The Lobby uses the integrated Stage 3 2D scene. Stage 2 is available in its own interaction sandbox; the Stage 1 foundation scene is preserved.

- `Battlefield2D/MapRoot` has BattlefieldMap2D references to the camera, logical playable area, path, slots and runtime mount points.
- `PlayableArea2D` queries an explicitly assigned Collider2D. The initial BoxCollider2D is 20 by 10 units; a PolygonCollider2D can replace its shape independently of the table Sprite. Off-table points are rejected.
- `WorldCamera/TopDownCameraFrame` uses an orthographic XY view and expands the view for the configured logical bounds, minimum size, padding and aspect ratio.
- WaypointPath has a per-path Plane setting. The retained scenes use XY. Edit its ordered point Transforms, starting on the right and ending on the left. EnemyPathFollower reuses the same distance-carry and movement-modifier rules.
- Add/move/remove instances of `Prefabs/TopDown2D/PlacementSlot2D.prefab`, then update BattlefieldMap2D.PlacementSlots. Slot colliders and the separate PlacementPoint are editable. The detachable sprite view observes existing TowerPlacementSlot available/highlighted/occupied state; pointer selection comes in Stage 2.
- `EnemySpritePlaceholder` and `TowerSpritePlaceholder` have detachable Visual children and independent HealthBarAnchor/ProjectileOrigin mount points. The blue Tower Visual Preview is a static illustration, not a placed or attacking Turret. The red movement sample reads BasicEnemy's existing speed but is not a combat Enemy and does not affect registry/rewards/Base HP.
- Sorting uses the Default layer with orders: tabletop 0, path 10, slots 20, reserved ground effects 30, units 40, reserved world feedback 60, screen-space HUD 100. The reserved HUD is empty until later integration. Background/table uses no collider; the separate playable shape and slots use Physics2D colliders on existing GameplaySurface/PlacementSlot layers. There is no world-input query yet.

Validation commands in Unity: **Defense → 2D Migration → Validate Stage 1 (Edit Mode)** with the new scene open, then **Validate Stage 1 (Play Mode)**. The Play Mode test runs the diagnostic route to its endpoint and exits Play Mode automatically. These do not initialize player save services. Stable gameplay asset IDs and save formats are preserved. Reports and previews are in the Git-ignored Documentation/Report folder; supplied specifications are archived in Documentation/Prompt.

## Getting Started and Controls

1. Open the `GameDesign` folder in Unity Hub, open `Assets/Defense/Scenes/MainMenu.unity`, and enter Play Mode.
2. Select **Start Game → Lobby**. Navigate with **A / D** or click the left or right stage card. Holding a key repeats navigation every 0.15 seconds after an initial 0.4-second delay.
3. Once the selected stage is centered, press **Enter** or click **Enter Stage** for the configured 2D scene. The list contains stages **1-1**, **1-2**, **1-3**, **Test Stage**, and **Test Stage 2**. Both Test Stages are always unlocked. Only campaign stage 1-1 is initially unlocked.
4. Open **Deck** and drag a tower or skill card from the left page onto a T1–T6 slot on the right. Use **Preset 1 / Preset 2 / Preset 3** to switch decks in the Deck Editor. The Lobby only previews the selected deck. Valid edits and the active selection save immediately. Select **Back to Lobby** to return.
5. Enter the stage to observe enemies moving from the right side of the tabletop toward the Base on the left. Select a Tower Card and click a green slot to place it. Successful placement pays costs and rotates the common Hand/Queue. For skills, hold LMB on the table to aim and release to cast; release over a card or any other UI to cancel.
6. Use **Pause / Esc** to pause or resume. Your deck is preserved when returning through Pause → Exit or Game Over → Lobby. **Retry** on the Game Over screen starts a fresh run of the same stage. The finite Test Stage shows **Stage Clear → Replay / Return to Lobby** on success.

Settings is a placeholder screen. Closing it returns to the screen that opened it: Main Menu, Lobby, Deck Editor, or Pause.

## Data and Current Assumptions

- `Assets/Defense/Resources/GameCatalog.asset` defines stage order, the tower and card catalogs, and the initial T1–T6 assignments.
- There are 15 tower types. The initial deck is `[Test1, Test2, Test3, Test4, Test5, Test6]`. PlayerDeck always contains six unique CardDefinition references, including at least one Tower Card.
- Available Cards is **the full card catalog minus the six cards in the current deck**. There are 17 cards (15 towers and 2 skills), so 11 are available. After replacement, the incoming card disappears and the outgoing card returns immediately. Duplicate assignments and removing the final Tower Card are rejected.
- T1–T6 are persistent deck-editing positions. At stage entry, a separate runtime copy is shuffled once. Hand and Queue changes do not modify PlayerDeck.
- Three independent deck presets and the active preset index survive application restarts. Progression and stage selection remain session-only. No PlayerPrefs are used.
- Stages `1-1` through `1-3` reuse the shared gameplay maps. Starting resources are 150, 175, and 200; enemy spawn intervals are 2.0, 1.9, and 1.8 seconds.
- `PlayerSession.CompleteActiveStage()` remains an idempotent progression-only operation. Finite stages call it on Victory; repeat clears leave completion unchanged and still award Gold. Game Over does not unlock stages. The three campaign test configurations still spawn continuously; enable Finite Spawning and configure Total Enemies on their StageDefinition assets to give them a clear condition.

## Deck Persistence and Presets

`PlayerSession.Deck` exposes `DeckPresetManager.ActiveDeck`; there is no separate active-deck copy. Each of the three presets is an existing PlayerDeck with six unique cards and at least one Tower Card. Switching immediately refreshes slot labels, Available Cards, selected-button styling, and Lobby preview, and cancels any in-progress drag. Combat still copies the active deck once per run; shuffling, Hand cycling and Retry never save battle state.

The save is `deck-presets.json` in `Application.persistentDataPath`. Its JSON contains `Version: 1`, a zero-based `ActivePresetIndex`, and three `Presets`, each containing six ordered `CardIds`. CardCatalog resolves the existing stable CardDefinition.CardId values; display names and catalog order are irrelevant. Keep IDs stable when renaming content.

First launch creates and saves three independent copies of GameCatalog.InitialDeck. Each loaded preset is validated through PlayerDeck. An unknown/missing ID, duplicate or invalid composition resets only that preset, preserving valid siblings. Malformed files or unsupported versions restore defaults with warnings. Invalid active indices reset selection to Preset 1. Repairs are saved immediately.

DeckPersistence delegates file I/O to the shared JsonFileStore, which writes and flushes a temporary file, then replaces the old save. A write failure retains both the in-memory edit and previous save and logs that persistence failed; it does not silently report success. The next valid edit or preset change attempts to save the complete current state again. Saves include only deck composition and active preset selection, not progression, Elixir, stage state or battle order.

No Scene, Prefab or Inspector wiring is required. The existing UI builders create the preset buttons. Unity's built-in JSON serialization module is enabled; no external package was added.

## Stage Clear, Gold, and Candy

The dedicated **Test Stage** uses the integrated 2D scene and is accessible at the right end of the Lobby carousel without campaign progression. It starts with 3 Base HP and 300 resources, spawns exactly five enemies at 0.75-second intervals, and awards 50 Gold on every clear. Its RewardTestEnemy definition uses 20 HP, speed 3, Base damage 1, resource reward 10, and a 100% chance of 1 Candy per combat kill. Let enemies through to test failure. BasicEnemy uses a 10% chance of 1 Candy. These are editable asset values, not hard-coded reward rules.

`FixedIntervalSpawner` counts successful spawns and exposes completion. `EnemyRegistry` owns the active population. `StageVictoryController` evaluates finished spawning plus zero active enemies in LateUpdate, after the final enemy's Base damage callback finishes. Base HP depletion triggers Game Over; a positive-HP Base arrival never triggers defeat. Optional scheduled combat-death statistics are independent of clear eligibility. `GameFlow.Victory` is terminal, mutually exclusive with Game Over, and freezes gameplay. Progression completion is separate from the once-per-run Gold award. Replay uses the existing scene reload, resetting the runtime Hand, Elixir, towers, skills, enemies and schedule while retaining currency, presets and session progression.

`PlayerSession.Currency` owns one `PlayerCurrency`. Gold and Candy are separate from stage resources and Elixir. Controlled add/spend operations reject negative amounts, unaffordable spending and integer overflow. `CandyDropSystem` subscribes to the spawner's combat-death event and rolls once using that enemy's definition. Base arrival and arbitrary object removal do not award Candy. Successful drops save immediately and survive Game Over; no pickup input or visual is required. Lobby and gameplay counters subscribe to currency changes.

`player-currency.json` under `Application.persistentDataPath` stores only `Version: 1`, `Gold` and `Candy`. The shared JsonFileStore also serves deck saves. Currency changes save immediately using a flushed temporary file and replacement; failure retains memory and the previous file, logs an explicit error, and exposes `LastSaveSucceeded = false`. A later valid mutation retries the complete current state. Malformed/unsupported saves reset balances with a warning; negative loaded balances are clamped and saved. Stage completion/unlocks remain **session-only** and are not restored after application restart.

No manual Scene/Prefab rewiring is required: StageInitializer adds and connects the reward components, and existing UI builders create the counters and Victory panel. No new package, shop, wave framework, pickup object or final visual asset is introduced.

## Default Statistics

| Tower | Damage | Range | Attacks per Second | Cost |
|---|---:|---:|---:|---:|
| Vinegar | 10 | 3 | 1 | 50 |
| Olive Oil | 30 | 5 | 0.4 | 75 |
| Hot Sauce | 5 | 2.5 | 3 | 100 |

Stage 1-1 starts with 10 Base HP, 150 resources, and a 2-second enemy spawn interval. The basic enemy has 40 HP, a movement speed of 1.5, Base damage of 1, and a kill reward of 10 resources. Edit **Resource Reward** in `BasicEnemy.asset` to configure the reward for that enemy definition.

Green placement slots have available, highlighted, and occupied presentation states. Available slots highlight when hovered with a Tower Card selected. HP bars appear above the red enemies. All 15 tower types share the existing BasicTurret prefab and combat code. Scroll the left page of the Deck Editor to view additional cards. All tower statistics are configurable in the corresponding definition assets.

## Hand Card Selection Toggle

Clicking a Hand card selects it; clicking that same runtime card again deselects it. Clicking another card switches directly. Both Tower and Skill cards use GameplayHand.ToggleSelection through the existing button listener. The explicit Select method remains available for idempotent programmatic selection.

Deselecting clears the card tint/context label, pending placement selection and residual slot highlight; existing selection events cancel any active Skill aim. It does not spend resources/Elixir, cast, place or rotate the Hand. A Skill gesture pressed on the world and released over a card still cancels aiming while retaining the selected Skill; only a subsequent deliberate card click toggles it off. UI raycast blocking and normal successful-use/reset behavior are unchanged.

## Placed Tower Range Inspection

With no Hand card selected, click an installed Tower to show its attack-range ring. Click the same Tower again to hide it, or another Tower to switch directly. Clicking ground, path, an empty slot, an Enemy, other UI or outside the battlefield clears inspection. Selecting any Hand card clears the ring immediately; existing placement, Skill gestures and same-card deselection retain priority. Deselecting a card never automatically inspects a Tower.

`PlacedTowerInspectionController` owns one optional runtime Tower reference. The existing placement pointer entry point forwards presses before processing Card actions; it reuses `BattlefieldPointer2D`'s UI blocking and XY conversion. The shared BasicTurret2D prefab has a separate trigger child, **Inspection Hit Area**, on the Default layer. Its 1×1.8-unit box is offset upward by 0.5 units to cover the placeholder and condiment silhouettes without changing the wooden plate's collider or combat targeting.

`Turret.EffectiveAttackRange` supplies both target eligibility and the ring radius. There are currently no range modifiers; the accessor returns the configured range. Inspection refreshes the target's root position and effective range each LateUpdate. The existing `SkillAreaVisual` draws 96 world-space XY vertices, with diameter twice that range, independent of sprite size/PPU, at sorting order 30 (above path/plates, below Towers). It has no collider or UI raycast surface. Existing Skill visuals remain separate.

Both TopDownStage2 and the integrated TopDownStage3 are wired. Pause, terminal states, Hand reset, target disable/destruction, scene exit and Retry clear inspection. No package or manual Inspector wiring is required for the saved scenes. `PlacedTowerInspectionSetup.Run()` is an explicit additive Editor authoring operation to restore these dependencies after rebuilding the native 2D scenes; save scene edits and leave Play Mode before invoking it.

`PlacedTowerInspectionValidation.Run` is an Editor integration runner for inspection input, effective XY geometry, UI priority, successful Tower/Skill actions and lifecycle cleanup. Use an isolated existing deck/currency profile through `DEFENSE_TEST_DECK_SAVE_PATH` when running this batch entry point. Local logs and a rendered Game View preview are kept in ignored `Documentation/Report/Placed_Tower_Range_Inspection/`.

## Cards, Elixir, and Skills

`CardDefinition` holds a stable ID, display name, placeholder color, and Elixir cost. `TowerCardDefinition` references the existing `TurretDefinition`; `SkillCardDefinition` references a `SkillDefinition`. Combat remains outside the UI.

Every run starts with **3 Elixir**, regenerates **0.36 per second** while Playing, and caps at **10**. Pause, Game Over and Victory stop regeneration. The vertical bar on the right smoothly follows the authoritative value. Retry resets it.

| Skill | Damage | Radius | Casting Time | Travel Time | Elixir |
|---|---:|---:|---:|---:|---:|
| Toy Throw | 60 | 1.5 | 0 s | 0 s | 3 |
| Slow Area | 0 | 3 | 0 s | 0 s | 2 |

Toy Throw reuses the existing area-damage asset (its stable script/asset identity remains CircularDamageSkill) and Enemy damage/reward API, filtering through the shared ellipse. Slow Area creates an 8-second zone with a 0.5 movement multiplier, followed by a separate 0.5-second visual fade. Leaving a zone removes its slow immediately; overlapping zones retain independent sources and use the strongest slow once, without multiplication. Enemy base speed is unchanged.

`BattlefieldPointer2D` converts the pointer through the configured camera to the XY map and rejects UI or off-map positions. One SkillDefinition.Radius and shared profile drive the same elliptical indicator and gameplay hit boundary. Slow membership uses XY geometry and movement/registry notifications, independent of visual sorting depth.

Both skills share the existing Hand/Elixir commitment pipeline. Empty-area casts still pay and cycle. Cancellation, invalid placement, or insufficient funds never spend Elixir or advance the cycle. CastingTime (before launch) and TravelTime (after launch) are separate delays; no projectile is rendered. The optional ThrowOrigin is captured separately from the target. Pause freezes both delays and area/fade timers; Game Over and Victory discard pending casts, clear transient skill markers/slow sources and freeze the remaining scene until Retry/exit.

Tower Cards currently cost **0 Elixir** and retain their existing resource costs. Their Elixir cost can be changed independently in the card asset. The default deck remains the original six towers; add Toy Throw and Slow Area through the Deck Editor.

Existing skill GUIDs and stable card IDs (`skill.fireball`, `skill.arrowrain`) are retained for reference compatibility; display names and asset paths reflect the new content.

Skill development values are editable in the corresponding SkillDefinition assets. Slow Area uses ActiveDuration, FadeDuration and SlowMultiplier; Radius is shared by logic and presentation. The integrated SkillCastingController references the existing BattlefieldPointer2D and PlayableArea2D; no new scene wiring or packages are required. An optional Throw Origin may be assigned on SkillCastingController for future projectiles.

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

Use **Defense → Run Prototype Validation (Play Mode)** outside Play Mode. The native 2D TopDownStage2/3/4 validators exercise camera/Physics2D/uGUI pointer queries, cancellation, Elixir and Hand cycling, AoE boundaries, delayed casts, and slow enter/exit, overlap, lifetime, fade and cleanup. TwoDOnlyValidation covers the scene/build/prefab cleanup contract and every retained stage entry/Retry route. DeckPersistenceValidation additionally covers preset independence, autosave/recovery/failure handling, actual UI switch/drop events, and active-preset stage/Retry integration. RewardValidation covers finite spawning, deferred Victory, duplicate protection, two clears, real Replay/Retry scene reloads, final lethal Base arrival, deterministic drop probabilities, currency notifications, disk recovery and write failures. The regression runner redirects deck and currency storage to a unique temporary path to protect player saves. Separate `RewardValidation.WriteRestartFixture` and `ReadRestartFixture` batch entry points validate currency restore across two Unity processes; set `DEFENSE_TEST_DECK_SAVE_PATH` to an isolated file in the same temporary directory for both runs. Reports are written to the ignored Documentation/Report directory. Automated pointer-coordinate tests are not manual mouse testing.


## Main menu artwork

The `MainMenu` scene uses `MainMenuUI` and the Inspector-assigned `Assets/Defense/Data/MainMenuSkin.asset` for the start-screen logo and Start Game / Settings / Exit button artwork. The stage carousel Lobby and Deck Editor retain their existing presentation and callbacks.

The original transparent source sheet is kept at `Assets/Defense/Art/UI/MainMenu/MenuSheet.png`. Ten standalone Single Sprites under `Sprites/` preserve its original RGBA pixels: Logo plus Normal / Highlighted / Pressed for each button. Button states map to source columns 1 / 2 / 3; keyboard Selected uses Highlighted, and Disabled uses the dimmed third-column art. Text labels remain available to existing UI tooling but their Text components are disabled so the baked lettering is not drawn twice.

Imports use Point filtering, no mipmaps and no compression. UI Images preserve aspect ratio. Proportional anchors keep the centered logo and vertical button stack inside the existing 1280 x 720 reference Canvas without changing its CanvasScaler or EventSystem. No extra Inspector setup is needed in the saved scene. `Defense.Editor.LobbySkinValidation.Run` checks the native uGUI state, click and navigation paths with isolated deck/currency test paths.


The Main Menu and stage-selection Lobby share the original `Assets/Defense/Art/UI/MainMenu/HarvestTabletop.png` background through `MainMenuSkin.Background`. `MainMenuUI` creates one non-raycasting Image as the first child of the existing Canvas. Its built-in AspectRatioFitter uses Envelope Parent to cover the screen at the source 1672:941 ratio, cropping the edges when needed without stretching. The existing start-screen and Lobby panels supply 8% and 20% black readability shades respectively; logo/button layout, event bindings and SpriteSwap states remain unchanged. The opaque Deck Editor and Settings panels retain their existing presentation.


The Lobby's top-left Back and top-right Settings buttons reuse `MainMenuSkin.LobbyBack` and `LobbySettings`. Six exact 536 x 192 RGBA crops under `Assets/Defense/Art/UI/LobbyCorners/` map source columns to Normal / Hover / Pressed; the full original sheet is archived alongside them. Selected uses Hover, while the third column is not assigned to Disabled. These two existing buttons retain their original anchors, positions, dimensions, callbacks and navigation. Their baked icon/label art uses Image.preserveAspect, Point filtering and no compression/mipmaps; redundant legacy Text rendering is disabled only on these controls. Settings-panel Back remains unchanged. The Deck Editor now reuses the same Back and Settings sprites through UiFactory.ApplyButtonSkin; its original Back to Lobby action, button names, positions, hitboxes and preset/card workflows are retained.


Lobby Enter Stage and Deck use `MainMenuSkin.LobbyEnterStage` / `LobbyDeck` and the same `UiFactory.ApplyButtonSkin` presentation helper. Six 720 x 240 Single Sprites under `Assets/Defense/Art/UI/LobbyActions/` preserve the supplied native 2172 x 724 source pixels; the narrower middle column has transparent padding. Normal / Hover / Pressed map to source columns 1 / 2 / 3, Selected uses Hover, and Pressed is not assigned as Disabled. The Enter Stage and Deck click targets are 420 x 78 (1.5 times their earlier 280 x 52 size); anchors and callbacks remain unchanged. Locked/unsettled stages are still rejected by the existing TryEnter logic; this UI does not newly disable the Enter Stage control.


The first three catalog entries in the Lobby carousel use `MainMenuSkin.stageBoards`: ordered 1-1 → Stage 1 art, 1-2 → Stage 2 art, 1-3 → Stage 3 art. These baked-in numbers are art ordinals, not replacement stage IDs. Actual stage names, Locked/Unlocked and Selected Stage remain in small metadata below the boards. StageCarouselView selects the bright center or muted side Sprite directly from the existing selected index; hover/press/lock never select the artwork column. Original movement, scale, hitboxes and input logic remain. Test Stage and Test Stage 2 retain the original cards until matching artwork exists.

`Assets/Defense/Art/UI/StageBoards/StageBoardSheet.png` is a native-resolution 1402 x 1122 Multiple sheet with six 624 x 288 Sprite sub-assets, Point filtering, no mipmaps/compression, transparent padding and centered pivots. The official built-in `com.unity.2d.sprite@1.0.0` package supplies the required Sprite Editor/data-provider authoring APIs. Import uses capability-checked providers and name/file-ID pairs; do not hand-edit its metadata.


### Pause menu artwork

The integrated `TopDownStage3` gameplay UI uses the Inspector-assigned `Assets/Defense/Data/PauseMenuSkin.asset`. Resume, Settings and Exit reuse `UiFactory.ApplyButtonSkin` with nine aligned 488 x 180 Single Sprites in `Assets/Defense/Art/UI/PauseMenu/`; the original 1448 x 1086 sheet is archived alongside them. Source columns 1 / 2 / 3 map to Normal / Hover (also keyboard Selected) / Pressed. Point filtering, no mipmaps/compression and Image.preserveAspect retain the artwork. Redundant Text components are hidden, while button names and callbacks remain intact. Centered 360 x 120 click targets sit at y=80, -50, -180; the title moves to y=210. The existing dark overlay, top-right Pause, Settings, Esc and timeScale logic remain unchanged. `TopDownStage3Setup` restores the skin binding when regenerating the scene. `Defense.Editor.PauseMenuSkinValidation.Run` verifies button states, native UI clicks, simulation pause/resume and exit navigation with isolated deck/currency paths.


### Result menu artwork and direct Pause Exit

Game Over uses Retry (source row 2) and Lobby (row 3); Victory uses Replay (row 1) and the same Lobby sprites. `Assets/Defense/Data/ResultMenuSkin.asset` supplies nine padded 488 x 180 Single Sprites from `Assets/Defense/Art/UI/ResultMenu/`. The shared button helper maps Normal / Hover and Selected / Pressed to columns 1 / 2 / 3, preserves aspect and hides duplicate Text rendering. Each result button retains its original name and callback, with a 360 x 120 click target centered at y=20 or -115. Titles and the Gold reward label are spaced above the buttons. The saved TopDownStage3 scene and setup script include this binding.

Pause Exit now calls the existing `GameFlow.ExitToMenu` directly, returning to Lobby without a confirmation dialog. Result replay/retry, rewards, progress and persistent data logic are unchanged. Related validation scripts reflect the direct-exit flow. `Defense.Editor.ResultMenuSkinValidation.Run` provides a short isolated Play Mode check of result states, native button clicks, restart/return routing, timeScale, reward idempotence and direct Pause Exit.
