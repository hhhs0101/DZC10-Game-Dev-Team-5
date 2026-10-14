# 3D Implementation — Stage 1

## Purpose

Convert the current Test Stage from a 2D visual representation into a 3D fixed-camera battlefield while preserving the existing gameplay architecture and rules.

The goal of this stage is **not** to redesign the game logic.

The main design principle is:

```text
Visual Representation = 3D
Gameplay Logic = Planar
Gameplay Plane = Tabletop XZ Plane
```

The battle should visually exist in a 3D Unity scene, but gameplay behavior should remain constrained to the tabletop surface.

Preserve the existing architecture and systems unless a change is explicitly required for this migration.

In particular, preserve:

- Card / Deck / Hand systems
- Elixir system
- Skill Card system
- Turret inheritance and combat logic
- Enemy inheritance and damage logic
- Stage progression
- Lobby
- Pause / Retry
- Path-following concept
- Tower Placement Slot concept

## 1. Core 3D Design Principle

The new battle scene should use:

```text
Visual Representation = 3D
Gameplay Space = Planar tabletop surface
```

The table surface acts as the logical battlefield.

For the initial 3D version:

```text
Gameplay Plane = XZ plane
Y axis = visual height only
```

Do not introduce gameplay rules that depend on vertical height yet.

The following should remain planar:

- Enemy path progress
- Turret range
- Skill radius
- Target priority
- Movement distance
- Placement validity

Use 3D Unity objects and coordinates, but keep gameplay reasoning based on the tabletop plane.

## 2. Playable Area

The playable area is restricted to the **top surface of the table**.

Conceptually:

```text
PlayableArea = Tabletop Surface
```

The space outside the table is visual background only.

Do not add interaction outside the tabletop.

The tabletop should later act as the surface used for:

- Tower placement raycasts
- Skill aiming raycasts
- Mouse-to-world conversion
- Valid gameplay-area checks

For Stage 1, prepare appropriate 3D Colliders and Layers so these systems can use the tabletop during the next migration stage.

## 3. Battle Scene Concept

Use the existing concept sketch as the layout reference.

Create a simple 3D test environment consisting of:

```text
Fixed Camera
    ↓
3D Table
├── Tabletop battlefield
├── Enemy path
├── Tower placement slots
├── Placeholder enemies
└── Placeholder towers
```

Use Unity primitives only.

Do not attempt final asset quality.

## 4. Table

Create a simple table using placeholder 3D geometry.

At minimum:

- Rectangular tabletop
- Simple supporting legs if useful for composition

The tabletop must provide a clearly identifiable horizontal battlefield surface.

Add a 3D Collider to the tabletop.

Create or prepare a dedicated Layer where appropriate, for example:

```text
GameplaySurface
```

This will later support mouse-to-world raycasting.

Do not put gameplay-state logic into the table object.

## 5. Fixed Camera

Use a fixed Perspective Camera.

The camera should show the table from an elevated diagonal angle similar to the concept sketch.

The player should be able to see:

- Most or all of the enemy path
- Tower Placement Slots
- Enemies
- Towers
- Existing gameplay UI
- Future Skill indicators

The camera must remain fixed during gameplay.

Do not implement:

- Camera movement
- Camera rotation
- Camera zoom
- Player-controlled camera

Camera transform and composition should remain configurable through the Inspector.

Avoid hard-coding camera coordinates into unrelated gameplay logic.

## 6. Enemy Representation

Replace the current 2D placeholder enemy representation with a simple 3D Sphere.

The Sphere is temporary visual content only.

Prefer a hierarchy such as:

```text
Enemy Runtime Object
├── Enemy logic/components
└── Visual
    └── Sphere placeholder
```

Do not attach core Enemy behavior directly to the temporary Sphere mesh if presentation and gameplay logic can remain separate.

This should make the Sphere easy to replace with a final enemy asset later.

Do not redesign the Enemy inheritance hierarchy.

## 7. Tower Representation

Represent Towers using simple 3D Cuboids / rectangular prisms.

These are temporary visual assets.

Prefer:

```text
Turret Runtime Object
├── Existing turret behavior/components
└── Visual
    └── Cuboid placeholder
```

Do not place core Turret logic directly into the temporary mesh object if it can be avoided.

The visual object should be replaceable without changing combat logic.

## 8. Enemy Path

Keep the existing waypoint/path-following system.

Do **not** replace it with NavMesh.

Represent the path using 3D world positions.

Use:

```text
Vector3 waypoint positions
```

while treating movement as planar movement across the tabletop.

For the current version:

```text
Y = tabletop height
```

during normal movement.

The route should:

- Start on the right side of the table
- Follow a predefined curved route
- End on the left side of the table at the Base/end point

Conceptually:

```text
Right
    -> curved predefined route
        -> Left / Base
```

The path should remain configurable for future stages.

Do not hard-code one universal waypoint sequence inside Enemy logic.

## 9. Path Visualization

Add a temporary visual representation of the actual enemy route.

Possible implementations:

- `LineRenderer`
- Simple path mesh
- Connected debug segments

The route visualization should derive from the configured path where practical.

Do not create an unrelated visual path that can become inconsistent with the real Enemy route.

The path visualization must be easy to:

```text
Enable
Disable
Replace
Remove
```

without changing Enemy movement logic.

## 10. Tower Placement Slots

Keep the existing predefined Tower Placement Slot system.

Do not switch to unrestricted free placement.

Placement Slots should exist as configurable 3D positions on the tabletop.

The Stage/Map configuration should determine the slots.

Conceptually:

```text
Stage / Map
├── Enemy Path
└── Placement Slots
    ├── Slot 1
    ├── Slot 2
    ├── Slot 3
    └── ...
```

Future stages must be able to define different:

- Slot counts
- Slot positions
- Path shapes

without rewriting Tower placement logic.

Do not globally hard-code placement positions.

## 11. Portable Visibility Helpers

Introduce temporary readability helpers for the 3D transition.

Examples:

- Enemy HP bars
- Path visualization
- Placement Slot highlights
- Selected-object highlights
- Future Skill AoE indicator support

These visual helpers must remain portable and loosely coupled.

They should be easy to:

```text
Disable
Remove
Replace
Reconnect
```

without modifying core gameplay logic.

Preferred responsibility split:

```text
Gameplay Object
    -> owns gameplay state

Visual Helper
    -> observes state
    -> presents feedback
```

Do not make gameplay systems depend on a specific debug visualization.

## 12. Enemy HP Bars

Adapt Enemy HP bars to remain readable from the fixed 3D camera.

Possible approaches include:

- World-space Canvas
- Billboarded HP bar
- Screen-space projection

Choose the simplest robust approach for the current prototype.

Enemy health calculation must remain independent from the HP bar implementation.

Removing or replacing the HP bar later must not require changing Enemy health logic.

## 13. Placement Slot Visibility

Placement Slots should be visually distinguishable.

Support visual states for at least:

```text
Available
Occupied
Highlighted / Hover-ready
```

The full 3D mouse-hover/input migration belongs to the next implementation stage.

For Stage 1, establish reusable presentation states and placeholder visuals.

## 14. Future Projectile Compatibility

Do not implement projectile combat in Stage 1.

Continue using the existing attack behavior where appropriate.

However, keep the 3D representation compatible with future projectile attacks.

Conceptually:

```text
Turret
    -> AttackBehavior
        -> DirectDamage
        -> ProjectileAttack
        -> Future attack types
```

Where useful, provide a clear firing origin in the visual hierarchy:

```text
Turret
└── Visual
    └── ProjectileOrigin / MuzzlePoint
```

This point may remain unused during Stage 1.

Do not make the new 3D Turret representation dependent on direct-damage-only assumptions.

## 15. Preparation for 3D Raycasting

The full input migration will be handled in the next stage.

However, prepare the scene now with appropriate:

- 3D Colliders
- Layers
- LayerMasks/configuration points

for at least:

```text
GameplaySurface
PlacementSlot
Enemy
```

Do not use `Physics2D` for new 3D world objects.

The next stage will explicitly migrate world input toward:

```text
Screen input
    -> Camera Ray
    -> Physics.Raycast
    -> Table / Slot / World hit position
```

Do not unnecessarily rewrite every input flow during Stage 1.

## 16. Y-Axis Rules

For this initial 3D version, the Y axis is not a gameplay dimension.

Treat Y primarily as visual placement.

Example:

```text
Tabletop Y = constant battlefield height
Enemy Y = tabletop + visual offset
Tower Y = tabletop + model offset
```

Do not add:

- Height-based range
- High-ground bonuses
- Height-sensitive targeting
- Terrain elevation gameplay
- Vertical path mechanics

Combat and movement remain logically planar.

## 17. Flying Enemies

Do not implement Flying Enemies during this migration stage.

All current enemies should behave as ground enemies following the tabletop path.

Preserve architectural extensibility for future Flying Enemies, but do not introduce:

- Air movement
- Height-sensitive targeting
- Separate aerial paths
- Flying-specific combat rules

until the base 3D implementation is stable.

## 18. Existing UI

Keep the existing gameplay UI as 2D screen-space UI where practical.

Preserve:

- Hand
- Elixir Bar
- Pause
- Base HP
- Card UI
- Game Over
- Retry
- Other existing HUD elements

The intended structure is:

```text
3D Battle World
+
2D Screen UI
```

Do not convert UI into 3D world objects without a clear reason.

## 19. Preserve Gameplay Rules

Stage 1 must not intentionally rebalance or redesign:

- Tower Damage
- Attack Rate
- Elixir
- Card cycling
- Skill values
- Enemy Health
- Enemy Speed
- Deck rules
- Stage progression

This stage is primarily a representation/world migration.

Avoid unrelated gameplay changes.

## 20. Stage / Map Extensibility

Design the Test Stage so future maps can configure their own world layout.

Keep at least the following stage-specific concepts configurable:

```text
Camera setup/reference
Table / battlefield object
Enemy spawn point
Enemy path waypoints
Base/end point
Tower Placement Slots
```

Do not assume future stages will use identical waypoint coordinates or placement layouts.

## 21. Stage 1 Scope

### Implement Now

- Fixed 3D camera
- 3D tabletop
- Sphere Enemy visual
- Cuboid Tower visual
- 3D waypoint/path representation
- Right-to-left route
- Configurable 3D Placement Slot positions
- Portable HP/path/slot visibility helpers
- 3D Colliders and Layers for future raycasts
- Preserve existing gameplay rules and systems

### Do Not Fully Implement Yet

- Complete 3D mouse-input migration
- Full Tower placement raycasting
- Full Skill aiming raycasting
- Projectile combat
- Flying enemies
- Vertical gameplay
- Terrain elevation mechanics
- Final environment assets
- Final Enemy/Tower assets
- Character animation
- Final VFX

## 22. Verification

Verify at minimum:

1. Test Stage loads as a 3D scene.
2. Camera remains fixed.
3. Tabletop is visible and functions as the conceptual battlefield.
4. Enemy placeholder is a Sphere.
5. Tower placeholder is a Cuboid.
6. Enemies follow the predefined 3D waypoint path.
7. Path starts on the right and finishes on the left.
8. Enemy movement stays on the tabletop plane.
9. Path remains configurable.
10. Placement Slot positions remain stage/map configurable.
11. Placement Slots appear correctly on the tabletop.
12. HP bars remain readable from the fixed camera.
13. Path visualization matches the actual configured route.
14. Visual helpers can be disabled without breaking gameplay logic.
15. Existing Card/Hand/Elixir systems remain intact.
16. Existing Turret/Enemy runtime architecture remains intact.
17. Tabletop and Placement Slots have suitable 3D Colliders/Layers for future Raycasts.
18. No Flying Enemy or height-based gameplay is introduced.

## 23. Implementation Report

After implementation, report:

1. Existing systems reused without modification.
2. Files created or modified.
3. Scene hierarchy changes.
4. Camera configuration.
5. Tabletop setup.
6. Enemy and Tower visual hierarchies.
7. Path configuration approach.
8. Placement Slot configuration approach.
9. Portable visual-helper architecture.
10. Layers and Colliders prepared for the next stage.
11. Automated tests performed.
12. Manual scene/play-mode checks actually performed.
13. Anything that could not be verified.

Do not claim visual or manual verification unless it was actually performed.
