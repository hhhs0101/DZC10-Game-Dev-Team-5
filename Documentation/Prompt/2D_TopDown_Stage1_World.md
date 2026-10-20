# 2D Top-Down Migration — Stage 1: Isolated Battlefield Prototype

## Objective

Build a **separate native 2D top-down test battlefield** using a top-down view of a tabletop. Reuse the current stage/map concepts without destroying the existing 3D battle scene.

Stage 1 validates the visual and spatial foundations, not complete battle functionality. Full 2D card/skill input migration occurs in Stage 2; progression and currency integration occur in Stage 3.

Read `2D_TopDown_Migration_Overview.md` before editing.

## 1. Inspect and Protect Existing Content

- Locate the current gameplay scene, camera, 3D tabletop/world scripts, Enemy path and spawn configuration, placement slots, Enemy/Tower prefabs and data definitions.
- Inspect the actual source for dependencies on `PlanarSpace`, `WaypointPath`, `EnemyPathFollower`, `TabletopSurface`, `TurretPlacementController`, and other world-specific components. Names may differ.
- Create a distinct new 2D test scene, preserving the existing 3D scene and its references.
- Do not revert commits or restore an old 2D version as the working implementation.

## 2. Camera and Coordinate System

```text
Projection: Orthographic
Orientation: straight top-down; Camera forward aligned with battlefield normal
Gameplay plane: XY
Z: fixed for gameplay coordinates and used only for visual organization
```

Use a configurable camera framing/orthographic size so the **entire tabletop playable region** fits clearly in view. No perspective angle, isometric skew, orbit, rotation, or free camera control.

A conventional setup may place the camera at negative Z looking toward positive Z, but follow actual project conventions. Avoid assuming hard-coded camera coordinates elsewhere.

## 3. Tabletop Map

- Render a flat, top-down tabletop placeholder using a `SpriteRenderer`, simple texture, or clean solid-colored 2D shape.
- The image should depict the **top surface only**; 3D legs, thickness and perspective are not required.
- Add a distinct logical `PlayableArea2D` definition (e.g. a `Collider2D` outline/shape and/or configured bounds).
- The logical playable area corresponds to the usable tabletop surface. Off-table background is non-interactive.
- A rectangular tabletop can be used for the prototype, but playable-area shape must be replaceable by later maps without rewriting Skill/Tower code.
- Do not use a rendered Sprite boundary as the only implicit authority for gameplay bounds unless it also provides an explicit queryable shape.

Suggested conceptual scene tree:

```text
Battlefield2D
├── WorldCamera (Orthographic)
├── MapRoot
│   ├── TabletopVisual
│   ├── PlayableArea2D
│   ├── WaypointPath2D
│   └── PlacementSlots2D
├── EnemySpawnRoot
├── EnemyRuntimeRoot
├── TowerRuntimeRoot
├── SkillEffectRoot
└── Existing Screen-Space UI (connected later)
```

Adapt to current project conventions; avoid redundant scene managers.

## 4. Enemy Waypoint Path

Keep the current **waypoint-following design**, not NavMesh or free navigation.

- Represent waypoints in the XY plane (prefer map-local transforms or configured `Vector2` positions).
- Initial path starts on the **right** side of the tabletop and terminates at the **left/base** side; intermediate bends are configurable.
- Preserve path progress, movement speed semantics and end-of-path events.
- Only the coordinate mapping and visual movement representation should change.
- Use a simple 2D circular Enemy Sprite/placeholder for visual validation.
- Use the actual waypoint sequence as the source for an optional path visual. Do not manually draw a different route.
- Configure the path per map/stage; avoid hard-coded global path coordinates.

If the existing Enemy runtime is too tightly bound to 3D to reuse without implementing Stage 2, create the **smallest diagnostic sample** needed to validate waypoint movement; do not claim full Enemy gameplay parity at Stage 1.

## 5. Tower and Placement-Slot Placeholders

- Use a simple 2D rectangular/square Tower Sprite as a visual placeholder.
- Create fixed 2D placement-slot objects located on the tabletop but outside the enemy travel path according to configured map data.
- Slot count and XY positions must be editable for each map; no globally hard-coded fixed layout.
- Define replaceable slot visuals and states: `Available`, `Occupied`, and `Highlighted`.
- Add `Collider2D` hit regions for future Stage 2 interaction; their sizes should be appropriate to the displayed slot.
- Do not implement unrestricted placement.

## 6. Sorting and UI Foundations

Create consistent sprite sorting for at least:

```text
Background/Tabletop
Path
PlacementSlot
GroundSkillEffect
Units (Enemy/Tower)
WorldFeedback (HP bars, highlights)
ScreenSpaceHUD
```

Use Sorting Layers/order and/or Canvas sorting as appropriate. Gameplay Z must not be used as an unintended combat-distance axis.

Provide detachable, presentation-only placeholders for:

- waypoint/path visualization
- selected/occupied slot highlight
- 2D Enemy health bar anchor
- skill area visual anchor for Stage 2

Removing a helper must not break movement or combat logic.

## 7. Future Projectile Compatibility

Keep Tower/Skill visual roots configurable for future 2D projectile origins and trajectories. Do not add projectile gameplay or rewrite current direct-damage combat in Stage 1.

## 8. Explicit Scope Boundary

Implement now:

- isolated 2D test scene
- Orthographic top-down camera
- tabletop background and logical playable area
- editable XY waypoint route (right to left)
- sample 2D Enemy path movement
- replaceable 2D Enemy/Tower placeholders
- configurable 2D placement slots and colliders
- simple sorting and optional portable visuals

Do not implement in Stage 1:

- complete 2D mouse-to-world casting and placement
- real 2D targeting/damage conversion
- Toy Throw/Slow Area runtime migration
- full session/Hand/Elixir/economy integration
- changing default launch flow to new 2D scene
- removal of old 3D scripts/assets

## 9. Acceptance Gate

Verify:

1. The new scene loads as a **native 2D**, orthographic, top-down tabletop view.
2. The entire playable tabletop is visible with no perspective skew.
3. Sample Enemy Sprite follows the configured right-to-left XY waypoint route.
4. Path visual agrees with real path data.
5. Slot count and positions can be changed in stage/map configuration.
6. Slot visuals and Collider2D regions are present.
7. World feedback helpers can be disabled without affecting core behavior.
8. Original 3D scene is still present and usable.
9. No player saves, Card IDs, Stage IDs or progression data were reset or overwritten.

Report actual test outcomes and any functionality intentionally deferred to Stage 2/3. **Do not proceed if the scene prototype gate fails.**
