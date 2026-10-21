# Codex Implementation Prompt — Remove 3D Play and 3D Fallback; Retain Only the Current 2D Game

## Objective

The project has already migrated its playable battlefield to a 2D sprite-based implementation. **Remove the obsolete `Play 3D` and `3D Fallback` functionality and retire the old 3D runtime path. The final game must have one supported gameplay mode: the current 2D mode.**

This is a cleanup and routing simplification patch, **not** a reset to an old revision and **not** a request to rebuild the game. Work from the current project state, retaining all functional systems and recently added assets.

The current kitchen battlefield uses 2D sprites in an **angled / pseudo-top-down visual composition**; it is **not necessarily a geometrically perfect top-down view**. Do not change the current camera composition, 2D world configuration, or the intentional elliptical skill indicator/hit area while removing 3D functionality.

## 1. Inspect the Project Before Editing

Audit the actual repository rather than assuming these are exact class or file names. Identify and list:

- UI buttons/labels/actions named or equivalent to `Play 3D`, `3D Fallback`, `Play 2D`, `Play`, and gameplay mode switches;
- scene selection, game start, stage launch, Retry, Lobby return, and any fallback-routing code;
- `StageDefinition.sceneName`, other stage-to-scene mappings, scene constants, and any JSON or ScriptableObject stage references;
- `EditorBuildSettings`/build scene list, bootstrap objects, startup scene, and any demo/test launch code;
- 3D-only scene files, 3D-only prefabs, 3D camera controllers, 3D world/placement/path controllers, `Physics.Raycast` or 3D collider-based gameplay adapters;
- shared services, game state, card/deck/currency persistence, enemy spawning, tower data, and other dependencies referenced by both old 3D and current 2D content;
- all references to candidate assets through serialized scene/prefab references, `.meta` GUIDs, `Resources`, Addressables, tests, and scripts.

Produce a concise dependency map before deleting anything. `3D` in a file name or a comment alone is not sufficient evidence that an item is safe to remove.

## 2. Final Entry/Scene Routing (2D Only)

- Make the normal **Play / Stage Select / Retry** flow launch only the current, working 2D stage scenes.
- Remove the **`Play 3D` button, action, and related mode-selection UI**. If the screen has both `Play 2D` and `Play 3D`, retain a single appropriately labeled **`Play`** entry (or the existing stage-selection flow) that launches 2D.
- Remove the **`3D Fallback` path**, its UI label/entry, mode-switch logic, and any runtime branch that silently loads 3D when a 2D scene cannot be found.
- If a 2D scene or reference is missing, report a clear validation/runtime error. **Never silently enter a removed 3D scene.**
- Keep the current 2D kitchen scene, stage identities, 2D route, refrigerator-side spawn, enemy path, base destination, and wood-plate tower slots.
- Ensure every currently playable `StageDefinition` routes to its correct 2D scene. Check stage selection, direct launch, Retry, victory-to-next-stage (if present), and returning to Lobby.
- Do not arbitrarily route different stages to one default scene. If a stage has no working 2D scene, report it explicitly and preserve its stage data for repair rather than hiding the problem.

## 3. Remove Obsolete 3D Implementations Safely

After the references have been audited and migrated:

1. Remove obsolete 3D-mode UI wiring and mode enumerations/flags whose only function was toggling between 2D and 3D.
2. Remove 3D fallback loaders, scene names, legacy 3D input paths, and unused 3D scene transition helpers.
3. Remove 3D-only runtime components, prefabs, scenes, test/demo bootstrap objects, and assets **when confirmed not referenced by the active 2D game or other retained functionality**.
4. Remove old 3D scenes from build settings and remove obsolete references from active prefabs, ScriptableObjects, scene hierarchies, asset registries, and tests.
5. Remove dead imports, compile symbols, 3D-only serialized fields, unused instructions, and obsolete docs/UI text that suggest `Play 3D` or `3D Fallback` still exists.
6. Preserve Unity GUID/reference integrity: modify/delete scene and asset files with their `.meta` files appropriately; do not blindly delete files or regenerate GUIDs for retained assets.
7. Update tests that intentionally asserted 3D entry behavior to assert the final 2D-only behavior; retain useful non-regression coverage.

**Important exceptions:**

- Do not delete a script or asset merely because it uses `Vector3`, `Transform`, `Camera`, `Collider`, or a third spatial dimension for **render sorting, UI, effects, or a shared engine API**. Inspect its real responsibility.
- Do not remove shared Tower, Enemy, Card, Stage, Skill, or persistence systems because old 3D prefabs also referenced them.
- Do not confuse the `3D Fallback` **scene/mode** with legitimate unrelated fallback behavior. Preserve, for example, legacy `StageDefinition` spawn settings when no JSON spawn plan is assigned.
- Do not restore an old 2D git commit or overwrite the current project with the earlier prototype.
- Do not introduce a new camera angle or enforce a circular visual range. The current view is intentionally angled, and the skill range is intentionally elliptical.

## 4. Preserve Current 2D Gameplay and Visuals

Keep the existing implementation and configuration of:

- kitchen/background and checkered route artwork;
- refrigerator-side enemy spawn and the current path to the base/player destination;
- wooden plate placement-slot sprite, placement validation, occupancy feedback, and tower visuals;
- condiment/seasoning Tower sprites, display names, TowerDefinitions, targeting, cooldown, attack ranges, and damage;
- vegetable EnemyDefinition assets, enemy roles/abilities already implemented, path following, damage, death, Candy drops, and base damage;
- Tower and Skill card selection, toggle-to-deselect, deck composition, three deck presets, Hand rotation, and Card IDs;
- click-to-inspect a placed Tower's attack range, including dismiss-on-outside-click or card selection;
- skill aiming/holding/release/cancel flow, Elixir charges, Toy Throw, and Persistent Slow Area;
- **the latest elliptical glow skill marker and the matching elliptical gameplay hit test** if already integrated: its visible ellipse and gameplay effect boundary must stay synchronized with `SkillDefinition.Radius`; do not revert it to circle-based damage or slow;
- Elixir regeneration, Gold rewards, Candy persistence, stage progression, finite-spawn Victory, Pause, GameOver, Retry, and returning to the Lobby;
- JSON spawn plan support if implemented, including per-entry delay, enemy ID lookup, malformed-plan errors, and the separate legacy spawning fallback.

Keep current gameplay data, numeric balance, art imports, sorting, prefabs, and scene layout intact unless a minimal change is necessary to repair broken references resulting from 3D removal.

## 5. Data Safety and Change Discipline

- Check `git status` before editing, and preserve user modifications. Create a safe rollback point (branch or other non-destructive checkpoint) before bulk cleanup; do not reset, clean, or discard uncommitted work.
- Identify stable `CardDefinition` IDs, Stage IDs, enemy IDs, save paths, and serialized asset GUIDs. Do not rename/recreate them simply to remove 3D mode.
- Do not wipe `PlayerPrefs`, persistent JSON files, currency, unlock progress, or presets.
- Prefer small, cohesive changes and an explicit list of removed files rather than broad search-and-delete.
- If shared 2D/3D code needs restructuring, keep working 2D behavior and serialization intact; only remove the obsolete 3D branch.
- If an old 3D asset still has an unexplained serialized reference, resolve its consumers first rather than deleting it blindly. Report any blocked deletion precisely.

## 6. Validation Requirements

Run the available compile/build/tests and, if a working Unity Editor connection is available, test relevant flows in Play Mode. Never claim a check was performed if it was not.

### Routing and Cleanup

- No reachable `Play 3D` or `3D Fallback` button, menu option, keyboard shortcut, mode flag, or active scene route remains.
- Normal Play/Stage Select/Retry loads the expected 2D battlefield.
- No removed 3D scene remains in build settings or active stage definitions.
- No `Missing Script`, unresolved asset GUID, scene-not-found, null-reference, or duplicate-controller error occurs in tested 2D flows.
- A project-wide search shows no **active** obsolete 3D mode/fallback references. If historical documentation remains, distinguish it from active code and report it.

### Gameplay Regression

- Tower Card select/deselect, wooden-plate placement, occupied slot rejection, and card consumption work.
- Placed Tower inspection range appears and dismisses as designed.
- Skill Card aiming, elliptical preview/effect inclusion, cast/cancel, Toy Throw damage, Persistent Slow Area entry/exit/expiry, Elixir payment, and Hand cycling work.
- Vegetable enemies spawn, travel their intended path, get attacked, die, and damage the Base if they arrive.
- JSON spawn plans (when present) and legacy spawn configuration (when no plan is assigned) both continue to work.
- Victory/GameOver/Pause/Retry and Gold/Candy awards remain correct.
- Deck presets, selected preset, currencies, and stage unlocks remain intact across restart where testable.

Record `Pass`, `Fail`, or `Not tested` for each relevant category. Existing unimplemented features should be labeled rather than presumed to work.

## 7. Required Implementation Sequence

1. **Audit:** identify entry points, scene references, 3D-only artifacts, shared dependencies, and a rollback checkpoint.
2. **Rewire:** make current 2D content the sole supported gameplay route and remove 3D/Fallback UI switches.
3. **Delete:** remove obsolete 3D-specific implementation only after reference checks and 2D wiring succeed.
4. **Verify:** compile/test, resolve broken Unity references, verify Play Mode where possible, search for remaining reachable 3D paths.
5. **Report:** provide a clear, factual final summary.

If a critical regression is detected, fix it before continuing broad deletion. Do not silently downgrade to 3D as a workaround.

## 8. Final Report Format

Provide:

1. Previous `Play 3D` and `3D Fallback` entry points found (actual symbols/files).
2. Final 2D-only scene and navigation flow.
3. Files/scenes/prefabs/assets **deleted**, **modified**, or **intentionally retained**, with reasons.
4. Shared dependencies and serialized references repaired.
5. Any stage definitions/build settings changed.
6. Regression tests run and their actual results, including Unity Editor/Play Mode vs code-only tests.
7. Save-data compatibility checks and any remaining risk.
8. Any obsolete 3D files not safely removable, with precise blockers.

### Acceptance Criteria

The user can start, play, replay, and exit the current 2D game without ever encountering `Play 3D`, `3D Fallback`, or any 3D scene. All implemented 2D combat, UI, stage, skill, and persistent data behavior remains intact. The current angled presentation and elliptical skill AoE must not be mistaken for obsolete 3D functionality.
