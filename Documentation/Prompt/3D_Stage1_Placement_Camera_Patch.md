# 3D Stage 1 Patch — Tower Placement Bug and Camera Adjustment

## Purpose

Apply two fixes to the current 3D Test Stage:

1. Identify and fix the Tower Placement bug.
2. Adjust the fixed gameplay camera to match the attached reference image more closely.

Read the existing project specifications first, especially:

- `3D_Implementation_Stage1.md`
- `Main.md`
- `Tower.md`
- `CardSystem.md`
- `HandCycle.md`
- `Skill.md`
- `Elixir.md`

Inspect the current Unity implementation before modifying code.

Do not replace working systems unnecessarily.

---

## 1. Tower Placement Bug

### Current Problem

The player can select a Tower Card from the Hand, but clicking a designated Tower Placement Slot does not place the selected Tower.

The Tower should be placeable on predefined placement slots, but the current 3D implementation prevents the placement from completing.

---

## 2. Diagnose the Root Cause First

Do not apply a workaround before identifying the actual cause.

Inspect the complete Tower placement flow:

```text
Select Tower Card
    -> selected TowerCardDefinition is stored
    -> player clicks Placement Slot
    -> input is converted into world interaction
    -> Placement Slot is detected
    -> slot validity is checked
    -> Elixir/resource validation occurs
    -> Turret is instantiated
    -> slot becomes occupied
    -> Card cycle advances
```

Determine exactly where this flow currently fails.

Pay particular attention to changes introduced during the 2D-to-3D migration.

Check at minimum:

- Whether the selected Tower Card remains selected after clicking the world.
- Whether the current placement logic still depends on `Physics2D`.
- Whether it still uses `ScreenToWorldPoint` in a way that assumes a 2D XY plane.
- Whether the new 3D Placement Slots have valid 3D Colliders.
- Whether the correct Layer / LayerMask is used.
- Whether the camera ray actually hits the Placement Slot.
- Whether the ray is blocked by the tabletop or another Collider.
- Whether Placement Slot objects are on the intended Layer.
- Whether the Slot reports `Available` correctly.
- Whether the Tower prefab/reference is correctly resolved from the selected Card.
- Whether Elixir/resource validation incorrectly rejects a zero-cost Tower Card.
- Whether UI raycast blocking prevents the world click from reaching placement logic.
- Whether the new XZ world position is converted correctly when spawning the Turret.

Report the identified root cause after fixing it.

---

## 3. 3D Placement Input

The 3D Test Stage should use 3D world interaction for Tower Placement.

The intended interaction is:

```text
Mouse Click
    -> Camera.ScreenPointToRay(...)
    -> Physics.Raycast(...)
    -> Placement Slot Collider
    -> TowerPlacementController
```

Do not use `Physics2D` for the new 3D Placement Slots.

Use the appropriate 3D LayerMask for placement detection.

Conceptually:

```text
PlacementSlot Layer
    -> contains valid Tower Placement Slot objects

Camera Ray
    -> only accepts appropriate placement targets
```

If the tabletop Collider is also hit by the ray, ensure that it does not incorrectly prevent a valid Placement Slot from being selected.

Use a clean LayerMask or hit-filtering strategy rather than fragile object-name checks.

---

## 4. Preserve Placement Rules

After the fix, preserve the existing Tower placement rules.

A Tower can only be placed when:

```text
A Tower Card is selected
AND
the clicked object is a valid Placement Slot
AND
the Placement Slot is Available
AND
the player can afford the Card cost
```

On successful placement:

```text
Instantiate selected Turret
-> place it at the Slot's configured placement point
-> mark Slot as Occupied
-> spend Card ElixirCost
-> advance RuntimeCardCycle
```

Current Tower Card Elixir costs are `0`, so a Tower with cost `0` must not be rejected by the Elixir system.

---

## 5. Failed Placement

If placement fails:

- Do not consume Elixir.
- Do not cycle the Card.
- Keep the selected Tower Card available according to the existing interaction rules.
- Do not mark the Slot as occupied.

Existing invalid-placement messages should remain functional where applicable.

---

## 6. Slot Placement Position

Do not assume that the root Transform of the Slot is always the exact Turret spawn position.

If useful, allow a configurable child/reference such as:

```text
TowerPlacementSlot
├── Visual
├── Collider
└── PlacementPoint
```

The Turret should be positioned using the configured `PlacementPoint`.

This keeps slot visuals and actual Turret position independently adjustable for future maps.

---

# 7. Camera Adjustment

Adjust the Test Stage camera using the **attached reference image as the camera-angle reference**.

The attached image is not a strict map-layout specification.

Use it mainly to reproduce:

- Camera direction
- Camera elevation
- Table framing
- Perspective feeling

---

## 8. Required Camera Orientation

The desired camera is **not an isometric corner view**.

The camera should face the table primarily from the front.

Do not significantly rotate the camera horizontally around the table.

Conceptually:

```text
Camera horizontal orientation:
Front-facing

Camera vertical orientation:
Looking downward toward tabletop

Approximate viewing angle relative to tabletop:
~40 degrees
```

In other words, the line of sight should form approximately a 40-degree angle with the tabletop surface.

The result should resemble the attached reference:

- Front edge of the table appears approximately horizontal.
- Left and right sides should not have a strong isometric skew.
- The full tabletop remains visible.
- The tabletop has enough visible depth to clearly read the 3D objects.
- Towers, enemies, path, and slots remain visually distinguishable.

---

## 9. Camera Must Remain Fixed

The camera remains a fixed gameplay camera.

Do not add:

- Player camera movement
- Rotation controls
- Zoom controls
- Orbit controls

The Camera Transform should remain editable through the Inspector.

Do not hard-code the exact camera coordinates into unrelated gameplay systems.

---

## 10. Camera Reference Priority

When matching the attached reference image, prioritize:

1. Front-facing orientation
2. Approximately 40-degree viewing angle to the tabletop
3. Full tabletop visibility
4. Clear readability of Path, Enemies, Towers, and Placement Slots
5. Minimal left/right rotational skew

Exact numeric Transform values do not need to match any specific value if the resulting composition better matches the reference.

---

## 11. Do Not Change

Do not modify unrelated systems as part of this patch.

Preserve:

- Enemy path-following
- Card/Deck/Hand architecture
- RuntimeCardCycle
- Skill Card behavior
- Elixir regeneration
- Enemy/Turret OOP hierarchies
- Lobby
- Stage Carousel
- Pause
- Retry
- Stage progression

Do not implement:

- Projectile combat
- Flying enemies
- Vertical gameplay
- Final environment assets
- Final Tower/Enemy models

---

## 12. Verification

After implementation, verify at minimum:

### Tower Placement

1. A Tower Card can be selected from the Hand.
2. Clicking a valid 3D Placement Slot detects that Slot.
3. A selected Tower is successfully instantiated on an Available Slot.
4. The Turret appears at the correct configured placement position.
5. The Slot becomes Occupied.
6. The same Slot cannot receive a second Tower.
7. A zero-Elixir-cost Tower can be placed correctly.
8. Successful placement advances the Card cycle.
9. Failed placement does not advance the Card cycle.
10. Failed placement does not consume Elixir.
11. Placement uses 3D Physics rather than `Physics2D`.

### Camera

12. Camera is front-facing rather than isometric.
13. Camera looks downward at approximately 40 degrees relative to the tabletop.
14. Table front edge appears approximately horizontal.
15. Full gameplay area remains visible.
16. Path, Towers, Enemies, and Placement Slots remain readable.
17. Camera remains fixed during gameplay.

---

## 13. Implementation Report

After completing the patch, report:

1. Root cause of the Tower Placement bug.
2. Files modified.
3. How the 3D Placement Slot detection now works.
4. Layers / LayerMasks / Colliders involved.
5. Whether any old `Physics2D` placement code was removed or replaced.
6. How Turret spawn position is determined.
7. Final Camera Transform values.
8. Approximate camera viewing angle.
9. Automated tests performed.
10. Manual placement/camera checks actually performed.
11. Anything that remains unverified.

Do not claim manual mouse placement or visual camera matching was verified unless it was actually tested.
