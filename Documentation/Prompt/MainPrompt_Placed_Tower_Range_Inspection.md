# Main Prompt — Add Placed Tower Attack-Range Inspection

Inspect the current Unity 2D top-down project before changing code. Read the attached `Placed_Tower_Range_Inspection_Patch.md` and implement it as written. Also review `Card_Selection_Toggle_Patch.md` and the existing Tower placement, Hand-selection, and 2D input architecture where available.

## Required Behavior

- **Only when no Hand card is selected**, clicking an already placed Tower shows its current attack range as a circle around that Tower.
- Clicking outside the inspected Tower clears the indicator; clicking a different placed Tower switches the indicator directly to that Tower.
- Clicking the same placed Tower a second time clears the inspection.
- Selecting **any** Tower Card or Skill Card hides the inspected Tower's range immediately and continues the normal Card action.
- Clicking other UI should dismiss inspection without causing click-through into world interactions.
- Preserve the existing same-card click-to-deselect behavior. Deselecting a Hand card does not automatically inspect a Tower.
- The circle must accurately match the Tower's **effective runtime attack range in XY world units**, not a separate graphic-only radius.
- The indicator is a replaceable, non-interactive visual; it must not affect combat or consume resources.
- Clean up selection/indicator state if the Tower disappears, the stage resets, or gameplay ends.

## Implementation Approach

1. Find the authoritative Hand selection state and inspect input routing.
2. Reuse existing runtime Tower attack-range data and any suitable preview renderer.
3. Use native 2D Tower hit detection (or the project's existing equivalent), accounting for overlapping wooden-plate slot colliders.
4. Add a small, centralized inspection controller only if necessary; do not duplicate the Card-selection system.
5. Wire clearing behavior to Hand selection, click-away, and gameplay lifecycle events.
6. Test both Tower Card placement and Skill Card aiming after the change.

## Scope and Safety

Do not rewrite Tower placement, targeting, Card/Hand cycling, Elixir, Deck presets, Skill casting, Enemy behavior, Stage Clear, Gold, Candy, or save data. This is a read-only inspection and visualization feature.

## Final Report

Report the files changed, click routing, inspection-state ownership, range-data source, visual scaling, non-regression checks, tests actually performed, and any unverified manual interactions. Do not claim manual Unity verification unless it actually occurred.
