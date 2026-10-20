# 2D Top-Down Migration — Stage 4: Regression, Switchover, and Cleanup

## Objective

Validate that the native 2D tabletop implementation preserves existing behavior, switch the main gameplay route to 2D **only after confirmation**, and safely retire no-longer-used 3D-specific presentation code without losing assets or saved data.

Prerequisites: Stages 1–3 completed and reported. Read `2D_TopDown_Migration_Overview.md`.

## 1. Functional Parity Matrix

Compare the latest working pre-migration version with the 2D implementation. For each item, document `Pass`, `Fail`, `Not tested`, or `Not implemented in original`; do not treat an unimplemented original feature as a migration regression.

| Area | Parity check |
|---|---|
| Lobby | Menu, Stage Select, stage lock/unlock feedback |
| Deck | Six-card uniqueness and min-one-Tower validation |
| Presets | Three independent presets, selected preset persistence |
| Hand | Shuffle, three-card Hand, success-only cycling, Retry reset |
| Placement | Slot-only installation, occupied-slot rejection, correct spawn point |
| Elixir | Start, regen, pause stop, cost payment and insufficient-resource feedback |
| Enemies | Spawn counts, movement speed, route completion, death semantics |
| Towers | Target policy, XY range, damage, fire intervals |
| Toy Throw | Aim indicator, cast/cancel, hit radius, damage, cost/cycle |
| Slow Area | Enter/exit, 8-second default lifetime, overlap sources, fade cleanup |
| Pause / GameOver | Input blocks, state transitions and resets |
| Victory | Finite-spawner clear, once-only completion and UI |
| Gold | Fixed Victory amount, repeat clear awards, failure gives none |
| Candy | Enemy chances/amounts, immediate auto-award, retained after failure |
| Saved data | Presets, selected preset, currencies, stage progression |
| Test Stage | Exactly five Enemies, all five defeats required, quick replayable victory |

## 2. Numerical/Spatial Verification

- Test multiple orthographic sizes and resolutions/aspect ratios to catch screen/world alignment bugs.
- Verify Sprite visuals and 2D Collider regions correspond.
- Verify targeting, Toy Throw damage and Slow Zone detection use the same **XY world-unit radius** as indicators.
- Test Skills on map edges, over path turns, near slots and under HUD boundaries.
- Verify rendering/sorting order for overlapping units, paths, skill zones and HP bars.
- Test pause/replay/reset while a Slow Area or its visual fade is active.
- Test enemy death or despawn while affected by overlapping Slow Areas.
- Verify right-to-left path direction and correct Base damage when enemies reach the endpoint.

## 3. Save/Restart Verification

Use copies/backups of representative existing player data where feasible.

Test this sequence:

```text
Choose Preset 2
Edit its Deck
Earn Candy from a death
Lose a run
Clear the repeatable Test Stage and receive Gold
Exit the application completely
Relaunch
Verify Preset 2 composition, active preset, Gold, Candy, stage progression
```

Do not mistake a scene reload for an application restart. Record the actual environment used (Editor Play Mode or built app).

## 4. Performance and Error Checks

- Watch Unity Console during scene loads, stage transitions, combat and repeated Retry.
- Identify lingering missing-script, obsolete Collider, LayerMask, NullReference and event-listener errors.
- Avoid both 3D and 2D runtime systems driving the same objects simultaneously.
- Check whether any 3D Physics/raycast path remains active in the 2D gameplay scene accidentally.
- Verify that repeated test-stage clears do not duplicate rewards, subscriptions, visuals or Enemy objects.

## 5. Safe Default Scene Switchover

Only after Stage 3 gate passes:

1. Update Stage/scene routing so the usual gameplay entry uses the new 2D battlefield.
2. Verify normal Stage Select, Retry, Lobby return and next-stage transitions.
3. Keep a clear fallback or reproducible checkpoint for the prior 3D scene until migration is accepted.
4. Do not change stable Stage IDs or save-file paths during the switch.

If a scene transition cannot be supported in 2D yet, leave it explicitly flagged rather than silently routing to a broken scene.

## 6. Cleanup

After confirming no active 2D references depend on a 3D-only component:

- remove or disable obsolete 3D-specific runtime logic from the **active 2D route**;
- keep shared domain scripts and data intact;
- avoid deleting 3D scene/prefabs/assets unless explicitly authorized;
- keep Sprite import settings, Layers, collision matrix and Sorting Layers documented;
- update internal docs and remove misleading comments describing the new XY battlefield as XZ/3D.

The cleanup should be conservative: avoid broad class rewrites or deletion of unrelated assets.

## 7. Final Acceptance and Handoff

A successful migration requires:

- native 2D top-down tabletop, Orthographic camera, XY gameplay and Physics2D spatial interactions;
- all implemented pre-migration gameplay systems preserved;
- no unintended changes to saved player progress;
- fully functional Tower placement and both Skills;
- end-to-end repeatable Test Stage, Gold/Candy rewards, Victory/GameOver and Retry;
- no critical Unity Console errors during actual tested flows;
- explicit documentation of any remaining regression or unverifiable condition.

## 8. Required Final Report

1. Summary of architectural changes from XZ/3D to XY/2D.
2. Files/scenes/prefabs created, edited, disabled, or deleted.
3. Spatial/input/physics conversions performed.
4. Domain systems reused without significant changes.
5. Feature-by-feature parity matrix with evidence and failures.
6. Automated test commands and results.
7. Manual Play Mode scenarios actually completed.
8. Save-data compatibility and full-restart results.
9. Current default gameplay scene/routing and fallback.
10. Known limitations and next work items.

Do not claim full migration success without the required verification; state partial completion precisely when needed.
