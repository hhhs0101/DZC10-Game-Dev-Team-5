# Placement Slot Wooden Plate Sprite Specification

## Purpose

Replace the current tower placement-slot visual with the provided wooden plate sprite.

This is a visual replacement task for the 2D battlefield. Existing placement behavior must remain intact.

---

## 1. Core Goal

Use the provided wooden plate image as the default visual sprite for tower placement slots.

The new plate sprite should represent valid tower placement positions in the battlefield.

This task changes the appearance of slots, not the core placement rules.

---

## 2. Preserve Existing Placement Logic

Keep the current placement flow unchanged:

```text
Select Tower Card
-> click a valid placement slot
-> validate placement
-> place Tower
```

Also preserve:

- valid placement checks
- occupied-slot rejection
- invalid-slot rejection
- Elixir validation
- successful placement flow
- card cycling after successful placement

Do not redesign tower placement logic unless a small integration change is required.

---

## 3. Use the Plate as Slot Visual

The provided wooden plate sprite should become the slot's SpriteRenderer image or equivalent visual asset.

Preferred structure:

```text
PlacementSlot
├── gameplay logic / collider
└── visual
    └── wooden plate sprite
```

If the current placement slot already uses a prefab, child visual, or serialized sprite field, reuse that structure where practical.

---

## 4. Scale and Readability

Adjust the plate sprite scale so it works well in the current 2D top-down kitchen/tabletop map.

Requirements:

- the slot should be clearly visible
- the slot should not overwhelm the battlefield
- the slot should fit visually beneath or behind the Tower
- the art should remain readable at gameplay camera size

Preserve the art proportions unless gameplay readability requires adjustment.

---

## 5. Clickability and Collider Alignment

If the current slot uses a Collider2D or another interactive hit area:

- preserve the current usability
- ensure the clickable area aligns reasonably with the visual plate
- do not make the slot hard to click

If necessary, keep the clickable area slightly more generous than the exact sprite outline.

Gameplay usability has priority over exact pixel-perfect matching.

---

## 6. Slot Visual States

If the slot currently supports states such as:

- idle / available
- hovered
- selected
- occupied
- unavailable

preserve those state behaviors.

The new wooden plate sprite should work with existing tinting, outline, overlay, or highlight systems where possible.

Do not remove useful visual feedback.

---

## 7. Occupied Slot Handling

Preserve the current behavior for a slot after a Tower is placed.

For example, if the current system dims, hides, marks, disables, or otherwise changes the slot after successful placement, preserve that behavior unless there is a good reason to adjust it.

A used slot must not look like a freely available slot.

---

## 8. Sorting and Rendering Order

Ensure the wooden plate sprite renders at an appropriate layer.

It should appear:

- above the battlefield background and path when appropriate
- below Towers, if Towers are intended to stand on it
- without incorrect overlap issues

Integrate with the current 2D sorting-layer / order-in-layer setup.

---

## 9. Reusability

If feasible, keep the slot sprite configurable rather than hard-coded.

For example:

```text
PlacementSlotVisual
-> serialized Sprite reference
```

or another existing asset/configuration path.

This will make future art replacement easier.

If the current architecture already supports configurable visuals, use that pattern instead of introducing a new one.

---

## 10. Scope

### Implement Now

- replace the slot visual with the provided wooden plate sprite
- preserve current slot gameplay behavior
- preserve clickability
- preserve state feedback where practical
- integrate with the current 2D map

### Do Not Implement Yet

- animated slot art
- multiple slot themes
- slot skin selection
- dynamic slot resizing system
- advanced slot effects
- new placement rules

---

## 11. Verification

Verify at minimum:

1. Placement slots display the wooden plate sprite.
2. The sprite scale looks appropriate in the current battlefield.
3. Clicking a valid slot still works.
4. Invalid placement still behaves correctly.
5. Occupied-slot behavior remains correct.
6. Towers render properly relative to the slot.
7. The slot remains visually readable.
8. Existing hover/selection/highlight feedback still works if present.

---

## 12. Implementation Report

After implementation, report:

1. Existing placement slot classes/prefabs reused.
2. Files or prefabs modified.
3. How the wooden plate sprite was assigned.
4. Scale/sorting adjustments made.
5. Whether collider/click area changed.
6. Whether slot-state visuals changed.
7. Automated tests performed.
8. Manual visual/placement tests actually performed.
9. Anything that remains unverified.
