# Codex Implementation Prompt — Add JSON-Based Stage Spawn Plans (Revised)

## Goal

Implement a simple, human-editable JSON spawn schedule in the **existing Unity 2D top-down kitchen defense project**. Stage authors should be able to set enemy order and time between spawns by editing a short JSON file rather than editing Unity's serialized `.asset` YAML or manually handling GUID references.

**This is an additive, backwards-compatible feature. Do not replace the existing stage, enemy, combat, or save architecture.** Inspect the real repository before editing; the classes and paths below come from a preliminary project inspection and should be verified against the current checkout.

**Implement the feature, not merely a feasibility assessment.** Work in the current Unity project, run the checks available in that environment, and report completed and unverified items separately.

## 1. Existing Project Integration Points

The project inspection identified these existing elements:

| Existing element | Required integration |
| --- | --- |
| `StageDefinition` (ScriptableObject) | Preserve all existing fields; add one optional `TextAsset` reference for the JSON spawn plan. |
| `FixedIntervalSpawner.cs` | Located at `Assets/Defense/Runtime/Enemies/FixedIntervalSpawner.cs` in the inspected checkout. It currently schedules spawns using a single interval. Adapt its scheduling path, or extract a **small scheduler helper used by the existing system**, to support per-entry JSON delays. |
| `EnemySpawner` | Reuse the existing actual enemy creation/spawn entry point. Do not create a second enemy-instantiation pipeline. |
| `EnemyDefinition` | Continue to use the existing ScriptableObject data for each enemy's prefab, stats, movement, abilities and rewards. |
| `EnemyRegistry` | This currently tracks **living runtime enemies**. Do **not** repurpose it as the lookup table for enemy definition assets. |
| Existing path and refrigerator spawn location | Keep the existing top-down map spawn point, waypoints, path following and base/destination handling. |
| Existing stage completion and rewards | Preserve finite-spawn completion rules, Base HP, victory/loss, repeatable Gold rewards and Candy drops. |

Inspect the implementations and caller relationships before changing them. Preserve serialized field names and references unless there is an unavoidable, explicitly justified migration.

## 2. JSON Authoring Contract — Keep It Minimal

Required authoring format:

```json
{
  "stageId": "test_stage_2",
  "spawns": [
    { "enemyId": "enemy_mushroom", "delay": 1.0 },
    { "enemyId": "enemy_tomato", "delay": 2.0 },
    { "enemyId": "enemy_potato", "delay": 0.5 }
  ]
}
```

Semantics:

- `stageId`: a human-readable stage identifier used for diagnostics and optional consistency checking. It is **not** a Unity scene name, asset path or GUID. Compare against the current stage's stable identifier only if one already exists; do not invent a new stage-loading system.
- `spawns`: an **ordered, finite** array of spawn events. Its length is the complete number of enemies to spawn in JSON mode.
- `enemyId`: a stable logical ID resolving to an **existing** `EnemyDefinition` asset, not its display name, asset filename or Unity GUID.
- `delay`: elapsed **gameplay seconds since the previous spawn**. For the first entry, it means seconds after the spawn phase starts. It is not an absolute time from stage start for every entry.
- The same `enemyId` may appear multiple times; every occurrence represents another enemy.
- Delay `0` is valid, preserves order, and may produce same-frame spawns. Negative, missing, nonnumeric, NaN and infinite delays are invalid. Blank/missing `enemyId` is invalid.
- An empty `spawns` list is considered a configuration error for this patch; do not accidentally grant a free stage clear.
- Do **not** add waves, enemy counts per entry, random spawn weights, repeat commands, infinite JSON schedules, hot-reload, or other DSL features in this implementation.

The example produces this timeline (relative to the moment the spawn phase starts):

| Time | Event |
| --- | --- |
| `1.0s` | Mushroom spawns |
| `3.0s` | Tomato spawns |
| `3.5s` | Potato spawns |

Enemy movement, survival, tower combat and earlier enemies still being alive must not delay the next scheduled spawn.

## 3. StageDefinition: Optional JSON Attachment

Keep `StageDefinition` and all existing serialized properties, including existing stage name, scene, base health, starting resources, unlock conditions, clear Gold reward, available towers, and legacy spawn fields:

- `enemy`
- `spawnInterval`
- `spawnSequence`
- `finiteSpawning`
- `totalEnemies`

Add **one optional serialized `TextAsset` field**, for example:

```csharp
[SerializeField] private TextAsset spawnPlanJson;
```

Follow the project's current coding conventions and access patterns. The actual name can vary if consistency requires it.

Authoring experience after initial implementation:

1. A developer writes/edits `Assets/Defense/.../SpawnPlans/TestStage2.json` in any text editor.
2. In the Unity Inspector, the developer assigns that JSON `TextAsset` to `TestStage2`'s `StageDefinition` **once**.
3. Pressing Play uses its JSON spawn schedule; future enemy order/delay changes require editing only this JSON file.
4. Stages **without** a JSON reference continue using their original settings.

The JSON is a Unity project asset. **Do not implement external JSON replacement in a compiled game, live file watching, or runtime hot-reloading.**

## 4. Explicit Spawn-Source Precedence and Legacy Compatibility

At stage initialization, select exactly one spawn source:

1. **JSON reference assigned:** validate and run JSON mode only.
2. **No JSON reference and `spawnSequence` populated:** use the existing ordered sequence with its existing fixed `spawnInterval` behavior.
3. **No JSON reference and no sequence:** use the existing single `enemy` repeat/fallback behavior, preserving `totalEnemies`, `spawnInterval`, `finiteSpawning`, and the current infinite-spawn semantics where supported.

For JSON mode:

- The plan is **always finite**, ending its spawn schedule after the last entry. Use an effective spawn-mode flag/state so stage completion recognizes this, even if the legacy `finiteSpawning` field has a different value. Do not require the author to change unrelated legacy fields as a second step to make the JSON work.
- Ignore the legacy fields **only for JSON scheduling**: `enemy`, `spawnSequence`, `spawnInterval`, `totalEnemies`, and the legacy `finiteSpawning` setting. Do not delete, overwrite or mutate their serialized values.
- Never append a fallback enemy or also run the legacy interval coroutine. Ensure there is exactly one active spawn scheduler.
- The total planned spawn count is exactly `spawns.Count`.

**Invalid assigned JSON must not silently fall back to legacy spawning.** Such fallback hides configuration mistakes and creates the wrong stage.

If a JSON plan is invalid, produce a clear configuration error and abort its spawn phase safely. **Do not trigger Victory, grant clear Gold, or leave the user in an apparently active but impossible-to-clear stage.** Reuse a suitable existing failure/error presentation where possible; otherwise surface a clear blocking error state and report the limitation. Do not award normal combat-loss rewards simply because loading failed.

## 5. Stable Enemy Definition Lookup (Not the Runtime EnemyRegistry)

Inspect existing `EnemyDefinition` assets and identifiers. Prefer the project's existing stable IDs if they are already suitable. Otherwise add the minimum data-oriented mechanism necessary, for example:

- A serialized stable `enemyId` string on `EnemyDefinition` (or existing equivalent), and
- A small serialized `EnemyDefinitionCatalog`/`EnemyDefinitionLookup` asset or configuration listing actual `EnemyDefinition` asset references.

The lookup is conceptually:

```text
"enemy_mushroom" -> existing Mushroom EnemyDefinition asset
"enemy_tomato"   -> existing Tomato EnemyDefinition asset
"enemy_potato"   -> existing Potato EnemyDefinition asset
```

Requirements:

- Use stable logical IDs independent of display labels (which may change) and prefab filenames.
- Make the definitions resolvable **in player builds**, not only from `UnityEditor.AssetDatabase`.
- Avoid runtime project-wide scene/asset searches if explicit serialized references solve the problem.
- Validate duplicate IDs, missing/blank IDs, unresolved referenced assets, or ambiguous mappings before any JSON spawn begins.
- Use pre-existing definitions and prefabs; preserve the stats, role-specific abilities (Basic/Support/Disruptor/Boss if present), sprites, death behavior, Candy drop setup, and current combat effects.
- Keep the runtime `EnemyRegistry` responsible for counting/managing spawned living enemies. If both registries exist, distinguish their names and responsibilities clearly.
- Populate only the IDs necessary for the project's real assets/sample initially; do not fabricate enemies that do not exist. If the examples do not match current IDs, use the actual valid IDs and document the mapping.

## 6. JSON Parsing, Validation and Scheduling

Implement a small serializable DTO/JSON loader using a library **already available** in the Unity project when possible (e.g. `JsonUtility` if its validation behavior is adequate). Do not add a heavyweight dependency unnecessarily.

Validate the complete file **before spawning the first enemy**:

- valid JSON object; required `spawns` array with at least one entry;
- every entry includes a valid nonempty `enemyId` and an explicitly present, finite, nonnegative numeric `delay`;
- every ID resolves to a usable `EnemyDefinition` and required spawn/prefab references;
- configuration errors include stage name/ID, JSON asset name, event index and invalid value where possible;
- a parse error must be distinguishable from a valid empty/different plan; beware of JSON deserializers that silently default missing numeric fields to zero.

Scheduling:

1. Stage initializes and chooses the JSON/legacy source.
2. If JSON is active, load, validate and resolve **all** events to definitions first.
3. Enter the existing spawn phase.
4. Wait the first event's `delay` in gameplay time, spawn that enemy via the current `EnemySpawner` at the map's existing spawn point, then continue through all remaining events in order.
5. Each next event waits its own `delay` **since the preceding successful spawn**. Treat frame scheduling tolerance normally; do not change the order or skip repeated entries.
6. When the last event has been spawned, mark **spawning complete** using the stage's existing mechanism. Do **not** call stage Victory immediately.

Pause/restart safety:

- Respect the project's actual Pause/Resume semantics; pauses must not consume relative spawn delay time. If the project uses scaled game time, the routine should use matching scaled time. Avoid accidentally using real time during pause.
- If a spawn is interrupted by Game Over, Retry, Victory, scene unload or stage teardown, cancel remaining events and clean up the routine/subscription using existing lifecycle conventions.
- Avoid double-running the schedule on scene re-entry or Retry, and avoid double-spawning due to repeated initialization.
- Invalid/error states must not accidentally advance the regular Victory condition.

It is acceptable to minimally extend `FixedIntervalSpawner` to support a resolved timed sequence, or to add a focused scheduler/helper behind that existing component. **Do not duplicate `EnemySpawner`, scene loading or combat systems.** Maintain existing fixed-interval behavior untouched for non-JSON stages.

## 7. Stage Clear, Defeat and Economy Regression

JSON scheduling is finite, but **all scheduled events being spawned is only one stage-clear prerequisite**. Continue to require whatever existing conditions apply, particularly no remaining living enemies once the schedule is complete. Preserve behavior for enemies that reach the Base instead of being killed.

Do not regress:

- Base HP damage and Game Over on path arrival;
- enemy path following from the refrigerator to the player/base destination;
- tower targeting, placement and range inspection;
- skill targeting/damage/slow effects;
- Card selection toggle, Deck/Hand cycling, Elixir and saved deck presets;
- finite stage completion and Retry;
- fixed Gold clear rewards once per successful run, including repeatable clears;
- per-enemy Candy drops and persistent currency behavior on victory/defeat;
- legacy fixed sequence, repeated-single-enemy and infinite modes.

Do not rebalance enemies, change Gold values, alter stage unlock logic, or edit game save files as part of this patch.

## 8. Safe Scope of Changes and Sample Plan

Target a small, maintainable patch. The minimal expected artifacts are:

- an optional JSON `TextAsset` reference in `StageDefinition`;
- a JSON DTO/model and parsing/validation code;
- stable definition lookup separate from the living-enemy `EnemyRegistry`;
- a JSON timed-plan scheduling path integrated into the existing `FixedIntervalSpawner`/`EnemySpawner` pipeline;
- one actual JSON example, preferably `TestStage2`, with its real `StageDefinition` reference assigned;
- any focused edit-mode/play-mode tests practical for the project.

Use the example file name `Assets/Defense/.../SpawnPlans/TestStage2.json` with a sensible real directory path chosen from the repository's organization. The sample should preserve the simple schema above and use IDs present in the actual enemy catalog. `TestStage2` was previously associated with scene `TopDownStage3`; verify the actual current association rather than assuming it is unchanged.

Keep the existing five-enemy regression/test stage and all other stage assets unchanged unless a tiny safe reference adjustment is essential. Do not rewrite Unity `.asset` YAML by hand if Unity's serialization/editor tooling is available. Do not delete/recreate pre-existing ScriptableObjects or prefabs (their GUIDs and links must survive).

## 9. Acceptance Tests (Run What You Can)

At minimum, verify the following, and distinguish automated, manual, and not-run tests in the final report.

### JSON functionality

1. A linked JSON file controls spawn order and delay; editing the JSON changes those values on the next run without modifying stage YAML/code.
2. Relative delays `1.0, 2.0, 0.5` produce approximately `1.0s, 3.0s, 3.5s` from spawn-phase start (subject to frame tolerances).
3. Repeated IDs create repeated enemies in the specified order.
4. `spawns.Count` defines the JSON spawn count; no `totalEnemies` expansion or fallback enemy is added.
5. A zero delay produces a promptly ordered spawn; a negative/missing/non-finite delay is rejected.
6. Unknown enemy ID, duplicate catalog ID, missing asset reference, malformed JSON and empty list cause explicit diagnostics and **not** an accidental Victory.
7. Pause halts spawn time and Resume continues the remaining delay correctly.
8. Retry/reload/defeat stops any prior schedule and does not produce duplicate spawns.

### Backward compatibility

9. No-JSON stage with nonempty `spawnSequence` retains its prior fixed-interval behavior.
10. No-JSON single-enemy fallback retains its prior `totalEnemies` behavior.
11. No-JSON infinite spawning continues with its existing behavior (if that mode exists in the checked-out project).
12. JSON mode works as finite independent of `finiteSpawning` legacy field and can clear normally when all required enemies are gone.
13. Victory/loss, Gold/Candy, Tower/Skill/Deck/Hand/Elixir and save compatibility are unchanged.
14. No runtime dependency on editor-only Unity APIs; project compilation and available test suites pass.

## 10. Final Report Required from Codex

After implementation, give a concise but precise report covering:

1. Which existing components and call paths you inspected and reused (`StageDefinition`, `FixedIntervalSpawner`, `EnemySpawner`, `EnemyRegistry`, `EnemyDefinition`, stage controller).
2. Files/assets added or changed.
3. Actual JSON file path and the `StageDefinition` that references it.
4. Actual `enemyId` -> `EnemyDefinition` mappings used in the sample.
5. Exact JSON-vs-legacy precedence and what happens to `finiteSpawning` in each mode.
6. Error behavior on an invalid assigned JSON plan.
7. Tests **actually executed** with pass/fail results and any screenshots/logs if available; list what was not tested.
8. Any remaining setup steps in the Unity Inspector or unresolved compatibility concerns.

**Do not stop at design advice or report success without making/verifying the project changes.** Preserve existing code, Unity metadata, scenes, serialized references and persistent game data.
