# 2D Top-Down Battlefield Migration — Architecture and Non-Regression Contract

## Objective

Migrate the **current** Unity game from a fixed-camera 3D tabletop battlefield to a **native 2D top-down tabletop battlefield**, preserving all gameplay systems and player data developed since the original 2D version.

**Do not revert the repository to an older 2D commit.** The current implementation is the baseline. Historical 2D code may be inspected as reference only.

The scope is the battle world's spatial representation, physics, mouse-world interaction, and scene/prefab presentation. This is **not** a gameplay redesign.

## Target Model

```text
Rendering: Unity 2D sprites / SpriteRenderer
Camera: Orthographic, directly above the XY battlefield
Gameplay plane: XY
Depth (Z): render organization only, not movement/combat
World input: screen position -> 2D world position -> Physics2D query
Playable area: visible top surface of a tabletop
Game HUD: existing screen-space Canvas/UI
```

A top-down table graphic is the initial map. The **battlefield must not be structurally coupled to a specific table texture, rectangular outline, or stage layout**: subsequent maps must be configurable independently.

## Architecture Boundary

```text
Existing domain/gameplay systems
  CardDefinition / PlayerDeck / DeckPresetManager
  Hand + UpcomingQueue / RuntimeCardCycle
  Elixir / Currency / Persistence
  Enemy state and damage / Turret stats and targeting policies
  Skills and effect definitions
  Stage spawning / Victory / GameOver / progression
                  |
       spatial and presentation adapters
                  |
Native 2D battlefield
  XY paths / 2D colliders / placement slot detection
  2D world aim / 2D skill areas / 2D distance queries
  sprites / sorting / HP bars / visual helpers
```

Inspect actual types and existing dependencies before choosing adapter/refactor boundaries; diagrams are conceptual, not mandates to introduce additional abstractions.

## Features That Must Survive

| Feature | Required behavior after migration |
|---|---|
| Deck | Six unique CardDefinitions, at least one Tower Card |
| Presets | Three independent, persistent deck presets and active selection |
| Hand | Three visible Cards, existing shuffled six-card rotation, successful-use-only cycling |
| Elixir | Start 3, max 10, regeneration 0.36/sec in Playing, zero-cost Tower support |
| Tower placement | Select Tower Card, place only on available map-defined slots; invalid actions do not spend/cycle |
| Tower combat | Existing targeting policy, damage, range, attack interval, Enemy HP and death |
| Skill input | Select, hold LMB to aim, release on playable area to commit; release on UI/origin Card to cancel |
| Toy Throw | Circular AoE damage and configurable radius; future casting/travel-time separation remains |
| Persistent Slow Area | Configurable radius, 8-second default active duration, slow only while inside, source-aware overlap, visual fade after expiration |
| Stage state | Pause, Game Over, Victory, Retry/Replay and finite-spawn completion |
| Test stage | Five configured Enemies, finite spawning, repeatable clear |
| Gold | Fixed reward on every stage Victory, including replays |
| Candy | Enemy-specific drop probability and amount, automatic persistent acquisition, retained on failure |
| Stage progression | First-clear record and unlock behavior; repeating a clear does not duplicate progression |
| Save data | Existing deck presets, active preset, Gold, Candy, and stored progression survive scene migration and restart |

**Important:** This is a preservation contract. Verify which items currently exist and work before changing them. Do not claim an item was verified merely because its specification exists.

## World/Screen Separation

- Domain systems must not be tightly coupled to a 3D Camera, `Physics.Raycast`, `Collider`, or XZ-specific helper.
- The 2D battlefield uses `Physics2D` only for spatial queries that belong to the 2D world.
- Continue to use ordinary world `Transform` objects where appropriate; gameplay positions and distance calculations are XY-based.
- UI continues to use Unity UI/EventSystem, not Physics2D, unless the existing project specifically requires otherwise.
- Use the same gameplay configuration values for visual skill radii and physics/query radii.
- Prefer map-provided spawn/path/placement data instead of constants embedded in Enemy or Tower classes.
- Preserve future compatibility with projectile-based attacks, without implementing new projectile mechanics during migration.

## Data-Safety Rules

1. Make a new working branch/checkpoint before edits. Do not silently overwrite the existing 3D scene/prefabs.
2. Build a separate 2D test scene and corresponding placeholders first.
3. Inspect existing IDs, file formats, serialized references, `Resources`/Addressables registries, and startup save-loading paths.
4. **Do not change stable Card IDs, Stage IDs, save locations, or existing saved currency/deck data as a side effect of the visual migration.**
5. Avoid resetting saved progress if the new scene temporarily lacks a component or serialized reference.
6. Do not clear PlayerPrefs, delete JSON save files, or reinitialize default decks during migration testing.
7. If a data migration is genuinely required, implement an explicit backward-compatible migration and test it against a copy of existing data.
8. Avoid editing old stage assets in-place until the 2D alternative is validated.

## Stage Plan and Gates

| Stage | Objective | Gate before continuing |
|---|---|---|
| 1 | Build isolated 2D tabletop world, camera, sprites, path, slots | Map renders and sample path-following works; existing 3D scene remains intact |
| 2 | Convert 2D placement, targeting, skill input/effects, and world UI | Tower and both Skills operate in 2D; rule and radius parity tested |
| 3 | Connect real session systems, progression, economy, full scene flow | Full five-Enemy Test Stage can clear, fail, replay; persistent data intact |
| 4 | Regression, comparison, scene switchover, and safe cleanup | Complete non-regression checks and explicit report of any unmet criteria |

**Implement sequentially.** Stop and report blockers or regressions at a stage gate; do not advance silently when a gate has not passed.

## Out of Scope

- Going back to old game logic or old commits
- Free tower placement instead of predefined slots
- New mechanics, balancing, upgrade shop, or Tower merge/synergy implementation
- Flying enemies or height-based combat
- New 3D/2.5D physics
- Final visual art or animation
- Rewriting currency, deck persistence, and progression systems simply because the scene is changing

## Required Reporting

For every stage, report:

- Existing classes/components reused
- Changes to source, scenes, prefabs and serialized asset references
- Actual tests run and results
- Manual checks performed versus unverified
- Regressions and known risks
- Whether the stage gate passed

Do not claim that Unity Play Mode, clicks, persistence across restart, or visual output were tested unless these checks were performed.
