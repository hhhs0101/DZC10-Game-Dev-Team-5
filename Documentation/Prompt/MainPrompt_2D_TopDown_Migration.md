# Main Codex Prompt — Native 2D Top-Down Tabletop Migration

You are working on the **current** Unity Tower Defense game. The goal is to migrate its battlefield from 3D tabletop to **native 2D top-down tabletop** while preserving all gameplay features, rules, and persistent player data implemented in the current version.

**Do not revert to an old 2D commit, rewrite the project from scratch, or discard any post-3D gameplay work.** Inspect and extend the current codebase.

## Read These Files

1. `2D_TopDown_Migration_Overview.md`
2. `2D_TopDown_Stage1_World.md`
3. `2D_TopDown_Stage2_Interactions.md`
4. `2D_TopDown_Stage3_Integration.md`
5. `2D_TopDown_Stage4_Verification.md`

Also inspect the actual repository and relevant existing specifications if present, especially those for:

- Tower / Enemy runtime
- Deck, HandCycle, CardSystem and Deck Presets
- Elixir and Skill Card behavior
- Toy Throw and Persistent Slow Area
- Stage Clear, repeatable Test Stage, Gold and Candy
- scene/session/persistence architecture

Treat the four migration stages as ordered milestones. Current source code is the authority for what is actually implemented; documentation expresses intended behavior. Report differences rather than assuming code already satisfies every prior specification.

## Active Milestone

**Implement Stage 1 only in the current run.**

Read Stages 2–4 for architectural constraints and upcoming integration requirements, but **do not implement their full work yet**. Stop after the Stage 1 gate, provide a report, and wait for explicit approval to proceed to Stage 2. For subsequent runs, change the active milestone to Stage 2, Stage 3 or Stage 4 as requested.

## Step 0 — Inspect and Protect

Before editing:

1. Identify the current battle scene, existing 3D world scripts, prefab/scene references, Camera and map configuration.
2. Identify `PlanarSpace`, `WaypointPath`, `EnemyPathFollower`, `TabletopSurface`, Tower placement, Skill casting, target range, persistent save initialization, and Stage routing (or their real equivalents).
3. Identify any differences between existing implementation and migration specification.
4. Establish a safe branch/checkpoint using the project's normal workflow before risky changes. Do not reset/revert unrelated work.
5. Preserve the current 3D scene and existing save files.

Do not use the old 2D commit as the implementation starting point. It is reference material only.

## Target Architecture

```text
Current gameplay/domain services (preserved)
     |
  XY/2D spatial interaction and 2D presentation (migrated)
     |
2D top-down tabletop scene
```

Target conventions:

```text
Camera = Orthographic, straight top-down
World plane = XY
Physics = Physics2D for world interaction
Map = top-down tabletop graphic + separately defined playable area
Movement = configurable right-to-left waypoint path
Placement = predefined, map-configurable 2D slots
Units = replaceable Enemy/Tower sprites
HUD = existing screen-space UI
```

No height/3D physics gameplay, isometric view, or free placement.

## Critical Non-Regression Rules

Preserve the existing behavior and values for:

- six-card Deck, three-card Hand, RuntimeCardCycle
- three persistent Deck presets and currently active preset
- Tower selection, valid slot placement and success-only card cycling
- Elixir costs, regeneration, Pause and Retry
- Toy Throw AoE damage and its cast/cancel flow
- Persistent Slow Area, exit cleanup, overlapping modifiers, 8-second default lifespan and post-effect fade
- Enemy HP, damage/death, Tower targeting and attack intervals
- Stage spawn/clear, Victory/Game Over and Retry
- fixed Gold clear rewards including repeat clears
- Enemy-specific Candy drops, immediate automatic acquisition and Candy retention on failure
- stable Card IDs, Stage IDs, save files, progression and currency values

Do not change game balance or introduce unrelated gameplay features as part of migration.

## Execution Requirements

- Keep the working 3D scene intact until the migration passes acceptance tests.
- Stage 1 uses a separate 2D test scene.
- Reuse existing domain types where practical and isolate changes to spatial/world/UI presentation dependencies.
- Prefer Inspector/stage/map configuration over hard-coded waypoint and slot coordinates.
- Keep placeholder visuals detachable/replaceable.
- Add focused tests where practical. Run relevant existing tests and verify compile/Console errors when your tools allow it.
- Do not claim mouse interaction or Play Mode verification unless tested through Unity.
- Do not alter save data or reset persistence in tests without explicit isolation/backup.

## Milestone Gates

**Stage 1 — 2D World:** Orthographic XY tabletop, configurable path/slots, sprites and sample path-following; original 3D scene untouched.

**Stage 2 — World Interactions:** Physics2D slot selection, XY targeting, Skill press/hold/release and precise AoE/Slow behavior work; no rule regressions.

**Stage 3 — Full Integration:** Deck/Hand/Elixir, five-Enemy Test Stage, Victory, Gold/Candy, Game Over/Retry, UI and saved data function end-to-end.

**Stage 4 — Verification:** Regression matrix, persistence restart test, switch default gameplay route to 2D, conservative cleanup and migration report.

Never silently skip a gate. Stop and describe blockers if a milestone cannot be verified.

## Deliverable Report for the Active Milestone

Report:

1. Current milestone and whether its acceptance gate passed.
2. Existing classes/components reused.
3. All files/scenes/prefabs created or modified.
4. Spatial/rendering/input decisions relevant to that milestone.
5. How the original 3D scene and persistent data were protected.
6. Tests actually run and their results.
7. Manual UI/Play Mode checks actually performed versus unverified.
8. Regressions, blockers, limitations, and exact next milestone.

Only implement the **active milestone**, then wait for the next instruction.
