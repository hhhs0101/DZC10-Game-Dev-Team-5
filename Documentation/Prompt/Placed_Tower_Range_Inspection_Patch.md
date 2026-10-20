# Placed Tower Range Inspection — Implementation Patch

## Objective

Allow the player to inspect the attack range of a **previously placed Tower** in the current 2D top-down battlefield, but **only when no Hand card is selected**.

Clicking a placed Tower displays its effective attack-range circle. The circle disappears when the player clicks elsewhere or selects a Hand card. Preserve the previously implemented Hand-card selection toggle and all existing combat interactions.

This is an inspection/display feature, not a new attack or Tower-upgrade mechanic.

## 1. Required Interaction Contract

```text
No Hand card selected, no Tower inspected
    -> click a placed Tower A
    -> show Tower A's attack range

Tower A being inspected
    -> click empty battlefield / path / placement slot / other world object
    -> hide range and clear Tower inspection

Tower A being inspected
    -> click any Hand card (Tower Card or Skill Card)
    -> hide range immediately
    -> process the card click normally

Tower A being inspected
    -> click a different placed Tower B
    -> switch inspection directly to Tower B
    -> show only Tower B's range

Tower A being inspected
    -> click Tower A again
    -> hide range and clear Tower inspection
```

Clicking any non-card UI control or non-battlefield area should also dismiss the inspection, while allowing that UI control's normal action to proceed.

At most **one placed Tower range** is visible at a time.

## 2. Hand-Selection Priority

The existing Hand card selection state remains authoritative.

When a Tower Card or Skill Card is selected:

- Do not enter placed-Tower inspection mode.
- Preserve the existing Tower placement and Skill aiming/casting behavior.
- Do not reinterpret a click on a placed Tower as inspection if that click is already part of an active Card interaction.
- Do not enable click-through of UI elements into the battlefield.

When a Hand card becomes selected, clear any previously inspected Tower **before or as part of** updating the Hand selection UI.

When a Hand card is deselected using the existing same-card toggle, no Tower is automatically inspected. The player must deliberately click a placed Tower afterward.

## 3. Inspect the Existing Architecture Before Editing

Locate and reuse existing components where practical:

- authoritative Hand Card selection controller and selection-change notifications
- `Card_Selection_Toggle_Patch.md` behavior
- placed Tower/Turret runtime objects and attack-range configuration
- Tower placement controller and slot click handling
- 2D world input routing, `Collider2D`, and `Physics2D` detection
- range indicator or preview visual components (if already present)
- pause, victory, game-over and stage-reset lifecycle hooks

Do not create an unrelated parallel Card-selection state machine.

## 4. Placed Tower Detection in 2D

Use the current native 2D world interaction architecture.

Recommended conceptual flow:

```text
LMB click (not consumed by UI)
    -> convert screen position to the 2D XY gameplay plane
    -> detect clickable placed Tower with Collider2D / Physics2D
    -> inspect that Tower if no Hand card is selected
    -> otherwise route input to existing Card action systems
```

Use the project's existing input abstraction if one exists; do not duplicate pointer-event processing.

If the visible SpriteRenderer does not have a suitable clickable collider, add a dedicated **2D selection hit area or child selection proxy**. Keep it independent from the Tower's combat range and from the underlying placement slot's collider. Do not change hit detection or targeting merely to make inspection work.

Prioritize clicked placed Towers correctly when their selection collider overlaps the wooden-plate placement slot. Decorative path and range visuals must not intercept clicks.

## 5. Inspection Selection State

Maintain one optional reference to the inspected runtime Tower, for example:

```text
InspectedTower: Turret? / TowerRuntime?
```

This must be separate from the existing selected Hand card reference.

Only one inspection state owner should decide when inspection begins, changes, or ends. A small `PlacedTowerInspectionController`-style component is appropriate if the project does not already provide this behavior.

Expose clean operations conceptually equivalent to:

```text
InspectTower(tower)
ClearInspection()
```

Avoid adding inspection state separately to every Tower prefab without a clear shared coordinator.

## 6. Range Source of Truth

The range circle must correspond to the actual targeting range used by the placed Tower **at runtime**.

Prefer the Tower's effective/current attack range, including any existing modifiers, over a static default value if those can differ.

```text
Tower effective attack range
    ├── gameplay target eligibility / distance check
    └── inspection circle world-space radius
```

Never hard-code an unrelated inspection radius or derive it from the bottle sprite's visual dimensions.

Use the same XY-plane distance metric and boundary semantics as the existing Tower targeting implementation. If the project has multiple targeting ranges or range-changing effects, display the currently effective range of the selected Tower and keep the indicator synchronized while inspection is active.

## 7. Range Indicator Visual

Show a simple, readable circular overlay centered at the Tower's **gameplay targeting origin / Tower world position**.

Recommended characteristics:

- thin ring, optionally with a lightly transparent fill
- clearly visible against the kitchen background and path
- non-interactive / no click-blocking Collider2D or UI raycast target
- appropriate SpriteRenderer Sorting Layer / Order or existing rendering abstraction
- independent of the temporary condiment Tower sprite art
- reusable/replaceable visual prefab or component

For a circle mesh/sprite whose unscaled diameter is `D` world units, compute scale so the displayed diameter becomes `2 * EffectiveRange` world units. Do not assume a particular Pixels Per Unit value or sprite texture size without inspecting the import settings.

The circle must remain centered on the Tower and match the actual attack radius in **world units**. Do not apply an arbitrary image-based scale factor that visually changes gameplay range.

Reuse an existing range-preview renderer if it can be cleanly configured for placed-Tower inspection; otherwise implement a minimal dedicated indicator without modifying Card aiming behavior.

## 8. Dismissal and Lifecycle

Clear inspection and hide the circle when:

- clicking empty ground or another non-Tower world object
- clicking another UI control outside the Hand
- selecting any Hand card
- clicking the inspected Tower again
- the inspected Tower is destroyed, sold, removed, or disabled
- leaving the gameplay scene
- Retry, stage reset, Victory, or Game Over

Follow the project's existing Pause behavior; inspection should not create input interactions while gameplay input is blocked. Clear inspection on entering Pause unless an existing consistent overlay policy requires otherwise.

Always avoid stale references, orphaned indicator objects, or visible range circles after the inspected Tower no longer exists.

## 9. Non-Regression Requirements

Do not modify the semantics of:

- the same-card Hand selection toggle
- Tower placement validation and occupied-slot checks
- Skill press/hold/release aiming and cancellation
- Elixir cost or regeneration
- Card cycling, Deck contents, and three-preset persistence
- Tower targeting, damage, attack interval, or attack strategy
- Enemy movement, HP, or death
- Stage Clear, Game Over, Retry, Gold, or Candy

Inspection must be read-only: it must never spend resources, place Towers, cast Skills, damage Enemies, or rotate cards.

## 10. Verification

Test at minimum:

1. No Hand card selected: clicking a placed Tower displays a circle.
2. The displayed circle matches that Tower's effective XY attack range.
3. The circle is centered on the actual Tower targeting origin.
4. Clicking empty ground hides the circle.
5. Clicking a path, placement slot, Enemy, or another non-Tower object hides the circle.
6. Clicking a non-card UI element hides the circle without preventing the UI action.
7. Selecting a Tower Card hides inspection and preserves Tower placement.
8. Selecting a Skill Card hides inspection and preserves Skill aiming/casting.
9. Clicking another placed Tower switches inspection directly; only one circle remains.
10. Clicking the inspected Tower again clears inspection.
11. Clicking Towers while a Hand card is selected does not override the Card action.
12. Deselecting a Card with the existing toggle does not automatically select a Tower.
13. Inspection input does not click through Hand/UI elements.
14. The range indicator does not intercept world clicks.
15. Destroying/removing/clearing the Tower removes the indicator safely.
16. Retry, Victory, Game Over, and scene exit leave no stale inspection state.
17. Inspection does not affect Elixir, Hand cycle, damage, or targeting.
18. A changed effective Tower range is reflected by the visual while inspected, if the game currently supports dynamic range changes.

## 11. Implementation Report

After implementation, report:

1. Existing classes and input pathways reused.
2. Files, prefabs, and UI objects created or modified.
3. How placed Towers are detected with 2D physics/input.
4. Authoritative source of the displayed attack radius.
5. Range indicator visual implementation and unit/scale conversion.
6. Selection priority between Hand cards and placed Towers.
7. Dismissal and cleanup behavior.
8. Automated tests performed.
9. Manual mouse/UI/visual checks actually performed.
10. Any remaining issues or unverified behavior.

Do not claim manual clicking or visual radius matching was verified unless it was actually tested in Unity.
