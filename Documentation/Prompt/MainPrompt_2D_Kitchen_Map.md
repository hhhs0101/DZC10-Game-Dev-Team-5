# Main Prompt — Integrate Kitchen Background and Gingham Enemy Path

Implement the attached art-based kitchen map in the current **real 2D top-down Unity battlefield**.

Reference files:

- `Cozy Pixel-Art Kitchen Interior (1).png` — background.
- `Pixel-Art Gingham S-Curve Track(1).png` — visible enemy path.
- `2D_Kitchen_Map_Layout_Spec.md` — detailed implementation specification.

## Required Level Flow

```text
Open refrigerator / left-upper path entrance
    -> Enemy spawn
    -> Follow the full red-and-white S-shaped cloth route
    -> Right-upper path endpoint
    -> Player/Base takes damage if an enemy reaches the destination
```

Do not reverse this direction. The route runs from **left to right**.

## Before Changes

Inspect the existing:

- 2D Stage scene and Orthographic Camera.
- Background and path SpriteRenderer setup.
- `WaypointPath`, `EnemyPathFollower`, and spawn configuration (or equivalents).
- Base HP and enemy-reaches-goal events.
- 2D PlacementSlots and Tower placement controller.
- 2D Skill cast input, Toy Throw, and Persistent Slow Area.
- Existing game-state/Stage Clear/Retry integration.

Reuse working implementations. Do not revert to historical code or a 3D world.

## Implementation Order

1. Import the two original PNGs as 2D Sprites. Preserve the route PNG's transparency and pixel-art clarity.
2. Render the kitchen as background and the gingham path above it; they have matching 1672 × 941 pixel canvases.
3. Set or preserve a fixed Orthographic top-down Camera, aligning the scene without distorting image aspect ratios.
4. Add or position a configurable Enemy spawn Transform at the fabric route's refrigerator-side entrance.
5. Add or position the Player/Base destination Transform at the route's upper-right endpoint; reuse the existing Base HP system.
6. Configure editable XY waypoints or a sampled spline following the **actual visual centerline** around every major curve. The route image alone is not movement data.
7. Connect the existing stage spawner and EnemyPathFollower to those waypoints, with left-to-right movement.
8. Keep PlacementSlots independently configurable and test tower placement.
9. Check Toy Throw and Slow Area targeting and gameplay radius alignment on the new map.
10. Check the existing Test Stage, Stage Clear, Game Over, Retry, Gold/Candy, Elixir, Deck/Hand, and saved presets for regressions.

## Constraints

- Do not create a player-controlled character. `Player/Base` means the current defensive goal.
- Do not implement NavMesh or autonomous pathfinding.
- Do not convert the scene back to 3D.
- Do not change combat balance or reimplement persistent data systems.
- Do not hard-code path coordinates or spawn/goal transforms inside Enemy types.
- Preserve the stage-specific configurability of spawn, goal, route, and placement slots.
- Preserve existing image files; make no artwork edits without explicit approval.
- If the images cannot be located in the Unity project, report the missing asset and request its exact local path rather than using placeholder art while claiming final integration.

## Deliverable

Apply the actual Unity project changes available to you, then report:

1. Files/scenes modified.
2. Background and route sprite setup and alignment.
3. Spawn/goal references and path configuration.
4. Tests run, distinguishing automated tests from manual Play-mode verification.
5. Regressions, blockers, and anything not verified.

Follow the detailed acceptance criteria in `2D_Kitchen_Map_Layout_Spec.md`.
