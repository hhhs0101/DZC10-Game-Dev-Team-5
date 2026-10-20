# Main Prompt — Replace Placement Slot Visual with Wooden Plate Sprite

Inspect the current Unity project before editing code or prefabs.

Relevant existing systems include:

- placement slot prefab(s)
- tower placement controller
- slot click/selection handling
- slot occupied/available handling
- 2D battlefield scene
- slot highlight / hover visual state handling
- SpriteRenderer and sorting configuration

Then read and implement:

- `Placement_Slot_WoodenPlate_Spec.md`

Use the provided wooden plate image as the new placement-slot sprite.

Treat that specification file as the source of truth.

## Main Goal

Replace the current placement-slot visual with the provided wooden plate sprite while preserving all current tower-placement behavior.

This is primarily a visual replacement task.

## Preserve Existing Functionality

Do not break or redesign:

- tower selection
- placement-slot validation
- occupied-slot validation
- successful tower placement
- invalid placement feedback
- Elixir cost handling
- card cycling after successful placement

The player interaction must continue to work as it does now.

## Implementation Requirements

- assign the provided wooden plate sprite to the slot visual
- reuse the current placement-slot prefab/visual setup where practical
- keep the gameplay collider/click area usable
- ensure the sprite scale fits the current 2D kitchen/tabletop map
- ensure sorting/layering is correct
- preserve hover/highlight/occupied feedback if the current system already has it
- keep the slot visually distinct from the background

## Architecture Preference

If the current slot art is hard-coded, refactor minimally so the slot visual can use a configurable sprite reference.

Prefer a clean structure such as:

```text
PlacementSlot
├── gameplay logic
└── visual sprite
```

Do not over-engineer this task.

## Testing

Verify:

- slot sprite appears correctly
- slot clickability still works
- tower placement still works
- occupied slots are still handled correctly
- slot-state visuals still work
- Towers render properly relative to the slot sprite

Do not claim manual visual verification unless it was actually performed.

## Final Report

After implementation, report:

1. Prefabs/classes modified.
2. How the new sprite was integrated.
3. Scale/sorting changes.
4. Whether the collider/click area changed.
5. Whether slot-state feedback was preserved.
6. Automated tests performed.
7. Manual tests actually performed.
8. Any remaining issues or unverified behavior.
