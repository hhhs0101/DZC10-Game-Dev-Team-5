# Card Selection Toggle — Implementation Patch

## Objective

Change in-game Hand card selection to a toggle: clicking the currently selected card again must deselect it. Apply the same behavior to Tower Cards and Skill Cards while preserving existing placement, casting, resource, and Hand-cycle rules.

## Required Interaction

```text
No card selected
  -> click Card A
  -> Card A selected

Card A selected
  -> click Card A again
  -> no card selected

Card A selected
  -> click Card B
  -> Card B selected (Card A deselected)
```

Only one card may be selected at a time. If the current selection is cleared, the Hand must display no selected card.

## Implementation Guidelines

1. Inspect the existing Hand UI, selection controller, Tower placement controller, Skill casting controller, and selection-state events before editing.
2. Implement the toggle at the authoritative card-selection state/controller, not independently in each card visual. Prefer a reusable operation equivalent to:

   ```text
   OnCardClicked(card):
       if selectedCard == card:
           ClearSelection()
       else:
           SelectCard(card)
   ```

3. Identify cards by the existing runtime card reference or stable identity mechanism; do not compare display-name strings.
4. `ClearSelection()` must reset the current selected-card reference/state and update all dependent presentation: selected border, tint/highlight, tooltips or contextual indicators that depend on selection, Tower placement preview/ghost (if present), and Skill aiming preview when applicable. Do not remove ordinary hover feedback.
5. Deselecting must not place a Tower, cast a Skill, spend Elixir or other resources, advance the Hand/Upcoming Queue, change Deck contents, or modify persisted presets.
6. Clicking a different Hand card must switch directly to it, without requiring a separate deselect action.
7. Preserve the existing Skill mouse workflow: selecting a Skill Card does not cast; holding LMB over the gameplay area aims; releasing on valid gameplay area attempts a cast; releasing over the originating card or other UI cancels the cast. A card click must never accidentally trigger a world action through UI click-through.
8. If a Skill is already actively aiming (mouse held), preserve its established release-to-cancel behavior; do not introduce a conflicting second-click action during the same pointer gesture. After canceling, restore a coherent selection/preview state using the existing controller. A subsequent deliberate click of the still-selected card must deselect it.
9. Preserve current behavior on successful use, scene exit, Pause, Victory, Game Over, and Hand refresh. Avoid retaining a stale selection when the selected card is replaced or removed from the Hand.
10. Keep the solution compatible with mouse-driven uGUI/EventSystem input and the project's existing card event hookups; avoid duplicate button listeners and double event handling.

## Non-Regression Requirements

Do not alter:

- Tower placement validation or occupied-slot rules
- Skill casting and cancel rules
- Elixir cost and regeneration
- Hand cycle / Upcoming Queue order
- Deck editing and three-preset persistence
- Enemy, Stage, Victory, Gold, or Candy systems

## Verification

1. With no selection, clicking a Tower Card selects it and shows the normal highlight/preview.
2. Clicking that Tower Card again clears selection and hides the placement preview, if any.
3. With no selection, clicking a Skill Card selects it.
4. Clicking the same Skill Card again clears selection and hides any idle targeting preview.
5. Clicking a different card switches selection directly, with exactly one highlighted card.
6. A deselect click does not spend Elixir, execute an action, or rotate the Hand.
7. Normal valid Tower placement still works after reselecting.
8. Normal Skill aim, cast, and cancellation work after reselecting.
9. A UI card click is not treated as a world placement/cast click.
10. Hand rotation or run reset does not leave stale selected-card UI/state.

## Implementation Report

After implementing, report the selection-state owner, files changed, how Tower/Skill visual states are reset, tests actually performed, and any manual UI interactions not verified. Do not claim mouse/UI behavior was manually tested unless it was actually tested.
