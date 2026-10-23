# Replace Energy Bar with Segmented Sprite and Integer Energy Regeneration

Update the existing Unity in-game Energy/Elixir UI and resource system using:

`ChatGPT Image Oct 23, 2026, 09_29_20 PM (2).png`

## Visual replacement

- Extract the **right-hand Energy bar** as the empty background, including the lightning icon and "Energy" label.
- Extract the **separate yellow block on the left** as a single reusable fill-segment sprite; do not leave the detached block visible.
- Display **10 identical yellow segments** within the dark bar, arranged from bottom to top without overlapping the wooden frame.
- At 0 Energy show 0 segments; at N Energy show the bottom N segments; at 10 Energy show all 10.
- Preserve transparency, original proportions, and crisp pixel-art rendering. Reuse one segment sprite rather than switching among pre-rendered `energy_00`–`energy_10` images.

## Energy logic changes

- Change the gameplay resource amount to **integer Energy/Elixir**, with `CurrentEnergy` and `MaxEnergy` as integers, `MaxEnergy = 10`, and the existing initial value (3) preserved unless current stage data explicitly overrides it.
- Change passive regeneration to **exactly +1 Energy every 2 seconds of active, unpaused gameplay** (0.5 Energy/second on average), replacing the old 0.36/second rate. Do not gradually add fractional Energy to the displayed or spendable amount.
- Use a separate floating-point elapsed-time accumulator or timer to track progress toward the next integer point. Preserve leftover elapsed time when a frame crosses the 2-second threshold; correctly handle long frames that span multiple intervals.
- Clamp Energy to 0–10. When full, do not accumulate hidden extra regeneration that would immediately refill Energy after spending.
- Pause the regeneration timer during Pause, Game Over, Victory, and other non-playing states, following the project's existing game-state conventions.
- Keep Energy costs and affordability checks consistent with integer values. Migrate any float-based card/skill cost consumers safely, without silently rounding non-integer costs: inspect and resolve such data explicitly if present.
- Deduct Energy only on a successfully committed action. Cancellation, invalid placement/casting, and failed actions must not spend Energy or advance the hand.
- Preserve existing deck/hand, tower placement, skill casting, stage restart, and game-state behavior.

## Integration and verification

Inspect the existing UI and Energy/Elixir classes before editing. Reuse their events/bindings and existing scene/prefab architecture where practical.

Verify:

1. At 0, 1, 5, and 10 Energy, the bar displays exactly that many segments from bottom to top.
2. During active gameplay, Energy increases by one after 2 seconds and by another one after 4 seconds, subject to the maximum.
3. Energy never has a fractional spendable value; timer progress remains internal.
4. Spending, insufficient Energy, cancellation, Pause/Resume, and max-cap handling work correctly.
5. No duplicate UI or old bar visuals remain.

Report modified scripts/assets, any integer-cost migration, and tests actually performed. Do not change unrelated gameplay systems.
