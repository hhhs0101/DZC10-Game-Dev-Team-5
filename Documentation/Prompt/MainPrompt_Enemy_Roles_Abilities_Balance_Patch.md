# Main Prompt — Implement Enemy Roles, Abilities, and Balance Patch

Implement the attached `Enemy_Roles_Abilities_Balance_Patch.md` in the **current Unity 2D top-down kitchen tower-defense project**.

First inspect the existing project rather than assuming specific class or prefab names. In particular, examine the current Enemy/EnemyDefinition inheritance or component model, damage and kill events, effective movement-speed modifiers, Tower attack controller, stage spawner, 2D colliders, and persistence IDs. Then apply the specification as an **additive patch**.

## Required playable first-pass roster

- Carrot — HP 100, speed 2.0, damage 100 — basic.
- Tomato — HP 85, speed 3.0, damage 100 — basic.
- Eggplant — HP 150, speed 1.7, damage 100 — basic.
- Garlic — HP 30, speed 2.0, damage 20 — nearby allies gain a configurable-area speed boost, with `speedIncrease = 1.5` (treat as 1.5× unless the current project has an established incompatible convention; report this decision).
- Broccoli — HP 200, speed 1.8, damage 30 — nearby allies share a finite, damage-absorbing shield pool of 1000 (report the interception and spillover rules).
- Potato — HP 150, speed 1.8, damage 20 — disrupt the nearest placed Tower by temporarily preventing attacks, using configurable duration/range/cooldown. Do not invent required tuning numbers.
- Boss — HP 2500, speed 1.2 — add its role/data but **do not invent a numeric damage value or a unique ability**. The input says `damage: tower health`; record this as an unresolved design decision and keep the incomplete behavior safe.

**Radish (HP 120, speed 2.3, damage 20, random-Tower disruption) is an alternative to Potato, not a second required implementation.** Document it, but do not implement/enable it without authorization.

## Constraints

- Reuse the current Enemy runtime, health pipeline, 2D waypoint movement, targeting, spawn/despawn, Candy, and Stage Clear logic.
- Respect the UML-like categories `BasicEnemy`, `SupportEnemy` (`SpeedSupportEnemy`, `ShieldSupportEnemy`), `DisruptorEnemy`, and `BossEnemy`, while using compatible components/data definitions if the repository architecture makes literal inheritance unsafe or duplicative.
- Keep modifiers and shield/disruption effects source-aware, reversible, and free of repeated frame-by-frame stacking.
- Preserve Tower combat and visual sprites, Card selection toggle, placed-Tower range inspection, Skills, Deck/Hand, Elixir, Gold/Candy, and save IDs.
- Keep the existing five-enemy repeatable test stage untouched. If needed, add a separate development-only test stage.
- Reuse actual matching vegetable art. Report missing sprites; do not assign unrelated assets.
- Do not change unrelated balance values or introduce a special Boss mechanic without approval.

## Work order

1. Audit the project architecture and identify existing assets, data definitions and damage/movement hooks.
2. Make the smallest safe code/data changes to implement the first-pass roster and abilities.
3. Add focused EditMode/unit tests and a practical test setup where possible.
4. Run available compilation/test checks and verify no regressions.
5. Report exactly which files changed, what was implemented, assumptions/TBD values, and which tests actually ran. Do not claim manual verification you did not perform.

The attached patch specification is the source of truth; do not request or require previous prompt files to complete this task.
