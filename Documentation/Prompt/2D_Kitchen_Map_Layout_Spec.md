# 2D Kitchen Map Layout — Refrigerator Spawn to Player Base

## Objective

Implement the supplied kitchen background and gingham S-curve path as the visual layout for the existing Unity 2D top-down battlefield. Enemies spawn from the open refrigerator at the upper-left, travel along the red-and-white cloth path, and approach the Player/Base at the path's upper-right endpoint.

**Preserve all existing game rules and systems.** This task changes the map layout, visual assets, and path configuration. It must not rebuild combat, cards, stage progression, or persistent saves.

## Supplied Art Assets

- `Cozy Pixel-Art Kitchen Interior (1).png` — the full kitchen background.
- `Pixel-Art Gingham S-Curve Track(1).png` — the visible enemy route, with transparency outside the fabric.

Both images are **1672 × 941 pixels**. Treat them as a matched composition, with the kitchen behind the path. Do not assume that the path texture itself is navigation data.

If the files have not yet been imported into the Unity project, ask for their local asset locations or use the supplied files when available. Do not fabricate file paths or substitute unrelated artwork.

## 1. Scene and Camera

Use the **actual 2D top-down scene** and its XY gameplay plane, not a 3D tabletop scene styled to look two-dimensional.

- Camera projection: Orthographic.
- Background: a SpriteRenderer, facing the 2D camera.
- Path: a SpriteRenderer above the background.
- Preserve the image aspect ratio, approximately 16:9, without stretching either sprite independently.
- Use matching Pixels Per Unit and matching world-space alignment for both sprites as a default starting point. Expose final transforms in the Inspector for manual tuning.
- Ensure HUD/Card/Elixir UI remains screen-space and does not change scale with the map artwork.

Recommended import settings for pixel art, if consistent with the existing project:

- Texture Type: Sprite (2D and UI).
- Sprite Mode: Single.
- Filter Mode: Point (no filter).
- Mip Maps: Off.
- Compression: None or another setting verified not to damage pixel edges.
- Preserve alpha transparency for the route image.

Avoid unnecessary changes to existing rendering pipeline or UI canvas configuration.

## 2. Layout and Sorting

Suggested scene hierarchy (adapt to existing project conventions):

```text
KitchenStage2D
├── Background
│   └── KitchenBackgroundSprite
├── Route
│   ├── GinghamPathSprite
│   ├── WaypointPath
│   └── OptionalPathDebugVisual
├── SpawnPoint (Refrigerator)
├── Destination (PlayerBase)
├── PlacementSlots
├── EnemyRuntimeContainer
├── TowerRuntimeContainer
├── SkillEffectContainer
└── ExistingGameplayUI
```

Use SpriteRenderer sorting layers/orders so that:

1. Kitchen background appears behind everything.
2. Gingham path appears on the floor above the background.
3. Ground skill effects and placement markers remain visible.
4. Enemy/tower/player sprites appear above the path.
5. HP bars and existing HUD remain readable.

The provided artwork is the layout reference. Do not automatically redraw the background or the path. If parts of the gingham path overlap cabinets or refrigerator artwork, first adjust scene alignment in the Inspector and document any persistent artwork overlap instead of distorting either image without approval.

## 3. Spawn Point — Open Refrigerator on the Left

Place the Enemy spawn point at the **left/upper-left end of the fabric route, visually connected to the open refrigerator**.

- Use an editable scene Transform, not a hard-coded Vector2 in Enemy classes.
- Enemies must enter the visible path from this location.
- The spawn position must match the first waypoint/path start.
- Keep the existing spawn scheduler, EnemyDefinition assets, and enemy runtime hierarchy.
- Do not automatically spawn new enemies from unrelated image decorations.

## 4. Destination — Player/Base on the Right

Place a configurable Player/Base anchor at the **right/upper-right endpoint of the fabric route**.

- Treat Player/Base as the existing defense objective with Base HP.
- Reuse existing 'enemy reaches base' and base-damage semantics.
- Do not add a freely movable player character or player movement controls.
- Use an editable scene Transform for the endpoint.
- A temporary 2D marker/sprite may represent the Player/Base until final art is available.
- Do not accidentally place an opaque Player/Base visual over large portions of the kitchen UI or path.

## 5. Route Geometry and Enemy Movement

Continue using the existing `WaypointPath` / `EnemyPathFollower` system or equivalent, on the **XY plane**.

**Important:** The red-and-white ribbon PNG is a visual guide, not a collision map or a generated navigation mesh.

- Create/map editable path points along the **visual centerline** of the cloth route.
- The first point corresponds to the refrigerator spawn.
- The final point corresponds to the right-side Player/Base anchor.
- Follow the visible full S-shaped route through both lower bends and its middle rise; do not replace it with a short straight line.
- Add enough waypoints or appropriately sampled spline points to prevent enemies from cutting across the fabric or taking unnatural shortcuts around curved sections.
- Maintain the existing configured Enemy movement speed, damage, HP, Slow effects, and path-following behavior.
- Debug/path visualization, if needed, must derive from the same path data and be independently toggleable.

The path must remain editable on a per-stage basis. Do not hard-code the same route for future maps.

## 6. Stage and Enemy Integration

Use the existing stage configuration and spawner to reference this map's spawn point, route, and destination. Support all currently implemented EnemyDefinitions, including the vegetable sprites, without creating separate movement logic per enemy type.

Preserve:

- Finite spawn schedules and Stage Clear conditions.
- Five-enemy repeatable Test Stage behavior if this map is used there.
- Game Over and Victory logic.
- Gold and Candy rewards.
- Elixir, Hand, Card rotation, and three saved Deck presets.
- Existing Pause and Retry behavior.

Do not silently change spawn direction back to right-to-left; the specified direction for this map is **left-to-right**.

## 7. Tower Placement and Skill Targeting

Keep existing gameplay interactions intact:

- Tower Cards select a tower and allow placement **only** on configured 2D PlacementSlots.
- PlacementSlots remain editable independently of the path art.
- Slots must not overlap the visible enemy route in a way that blocks readability or causes placement ambiguity.
- Existing LMB press/hold/release Skill aiming continues using XY coordinates.
- Actual AoE Damage and Slow Area radius must match the displayed 2D indicator radius.
- Slow Areas must continue to apply Slow while an enemy is inside, remove the source on exit, and clean up on expiration.

Do not implement a click-to-move Player or switch to arbitrary free tower placement.

## 8. Playable Area and Input

The gameplay world should occupy the intended kitchen/tabletop floor region. Treat furniture/cabinets as decorative background unless additional collision rules have already been established.

- Use the existing 2D gameplay-area validation or an editable 2D polygon/rectangular boundary.
- UI elements must continue to block world interactions appropriately.
- Do not assume every non-transparent pixel of the path texture is an input collider.
- Distinguish world interaction layers for PlacementSlots, Enemies, and any relevant gameplay surfaces.

## 9. Visual Scale and Pixel Art

- Keep vegetable Enemy sprites, 2D tower sprites, and Player/Base marker legible relative to the cloth width.
- Ensure enemy sprites travel visually **on** the path, not behind it.
- Do not stretch individual sprites to arbitrary aspect ratios.
- Prefer consistent Pixels Per Unit and world-scale choices.
- Keep future art replacement simple by separating SpriteRenderer/Visual children from gameplay logic where possible.

## 10. Scope Boundaries

Implement now:

- Kitchen background sprite in the 2D battlefield.
- Red gingham route sprite aligned over the background.
- Refrigerator left-side spawn point.
- Right-side Player/Base endpoint.
- Editable XY waypoints following the visible route.
- Existing enemy spawning and path-following connected to the new anchors.
- Existing placement and Skill interaction preserved.
- Focused scene/play-mode verification.

Do not implement as part of this task:

- New enemy behavior types or balance changes.
- New Player movement controls.
- Collision with individual kitchen furniture.
- New Stage progression rules.
- Rewrites of Gold, Candy, Deck persistence, or upgrade systems.
- Custom path-finding, NavMesh, or free roaming AI.
- Major edits to the supplied art images.

## 11. Verification and Acceptance Criteria

Verify all of the following where possible:

1. The camera displays the kitchen and entire usable route without clipping or distortion at the target aspect ratio.
2. Transparent areas of the path image reveal the kitchen background correctly.
3. Spawn appears near the open fridge at the upper-left path entrance.
4. Destination aligns with the upper-right path exit.
5. A spawned enemy follows the full cloth centerline left-to-right without visibly cutting across bends.
6. The enemy reaching the destination invokes the existing Base HP loss behavior exactly once.
7. Enemies can still be damaged, slowed, killed, and removed.
8. Tower Cards still install towers into valid 2D PlacementSlots.
9. Toy Throw aiming/damage and Persistent Slow Area aiming/effects still function.
10. Existing spawner count, Victory, Game Over, Retry, Elixir, Hand, Deck presets, Gold, and Candy remain unchanged.
11. The map's path, spawn point, destination, and slot positions can be edited for future stages.
12. Play-mode interaction checks are clearly distinguished from code-only or editor-only checks.

## 12. Final Report

Report:

1. Existing scene and systems reused.
2. Files and scene assets created/modified.
3. PNG import and sorting configuration.
4. Camera and sprite alignment settings.
5. Refrigerator spawn and Player/Base endpoint references.
6. Path waypoint setup and how it aligns with the visual route.
7. Any image/path overlap or alignment problems.
8. Tower placement and Skill targeting validation results.
9. Stage/Reward/Persistence regression test results.
10. Remaining unverified interactions and manual Unity Editor actions needed.

Do not claim screenshot alignment or mouse interaction was checked unless actually verified in the Unity Editor or a running game.
