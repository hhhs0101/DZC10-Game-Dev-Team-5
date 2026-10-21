# Enemy Roles, Abilities, and Balance — Unity Patch Specification

## 0. Objective and scope

Extend the **existing** Unity 2D top-down tower-defense Enemy system with the supplied Basic, Support, Disruptor, and Boss roles. Apply the statistics **exactly as specified** below; do not rebalance them. Keep the refrigerator spawn point, XY waypoint path, right-side player/base destination, existing Tower/Skill combat, Card/Deck/Hand, Elixir, Gold/Candy, stage lifecycle, and save files intact.

**This is an additive patch, not a rewrite or a Git rollback.** Inspect the current project before deciding which types and components to extend. Do not replace the previously created vegetable variants indiscriminately; reuse existing Tomato/Potato assets and identities where appropriate, and keep unrelated enemy types available.

### Status conventions

- **Specified:** directly supplied by the design and required.
- **Proposed interpretation:** a practical choice for an ambiguous design statement; make it editable and report it.
- **Open/TBD:** not supplied. Do not silently invent a game-balance value or an ability.

## 1. Required enemy roster

| Category | Enemy | HP | Base movement speed | Base-reach damage | Ability / other specified parameter |
|---|---|---:|---:|---:|---|
| Basic | Carrot | 100 | 2.0 | 100 | None |
| Basic | Tomato | 85 | 3.0 | 100 | None |
| Basic | Eggplant | 150 | 1.7 | 100 | None |
| Support (speed) | Garlic | 30 | 2.0 | 20 | Speed support; `speedIncrease = 1.5` |
| Support (shield) | Broccoli | 200 | 1.8 | 30 | Shared protection shield, `shieldValue = 1000` |
| Disruptor (selected baseline) | Potato | 150 | 1.8 | 20 | Throws dirt at the nearest Tower and disrupts it |
| Disruptor (alternative, **not baseline**) | Radish | 120 | 2.3 | 20 | Disrupts a randomly chosen Tower |
| Boss | Boss (identity/art TBD) | 2500 | 1.2 | **TBD: original text says `tower health`** | Whether it needs a unique ability is TBD |

**Do not simultaneously treat Potato and Radish as required enemies:** the original design presents them as alternatives ("or"). Implement **Potato** as the selected first option for this patch; retain Radish as a documented follow-up option only. The user can later switch the choice.

In this document, **base-reach damage** means the damage applied through the project's existing `Enemy -> Base` endpoint event when an enemy completes the path. If the existing project uses another interpretation for its enemy `damage` field, inspect and preserve that behavior and explain the mapping.

**Missing tuning parameters:** the Support radius for Garlic/Broccoli, the Potato ability cooldown/range/duration, and any Radish parameters have **no values** in the source design. Expose editable configuration fields without inserting arbitrary required balance numbers. Test behaviors with explicitly supplied test configurations; flag unconfigured runtime abilities clearly.

## 2. Existing architecture integration

### UML-aligned conceptual class structure

```text
Enemy
├── BasicEnemy
├── SupportEnemy
│   ├── SpeedSupportEnemy       (Garlic)
│   └── ShieldSupportEnemy      (Broccoli)
├── DisruptorEnemy             (Potato; Radish alternative)
└── BossEnemy                  (Boss)
```

The supplied UML describes the following common API (use existing naming/conventions if the project already has equivalent methods):

```text
Enemy
- hp: int
- speed: float
- damage: int
- enemyType: VegetableType
+ move(): void
+ takeDamage(amount: int): void
+ dealDamage(): void

SupportEnemy : Enemy
- supportArea: float

SpeedSupportEnemy : SupportEnemy
- speedIncrease: float
+ buffEnemies(): void

ShieldSupportEnemy : SupportEnemy
- shieldValue: int
+ protectEnemies(): void

DisruptorEnemy : Enemy
+ sabotageTower(): void

BossEnemy : Enemy
```

**Do not duplicate core logic** merely to match method names. Reuse the established `Enemy`, `EnemyDefinition`, health/damage pipeline, `EnemyPathFollower`, target acquisition, spawner, and status-modifier components. Implement thin role-specific subclasses where consistent with the repository; otherwise use reusable ability components attached through data-driven enemy definitions, maintaining the requested conceptual hierarchy and clearly reporting any deviation. Do not produce eleven copied Enemy MonoBehaviours.

Prefer data/configuration for per-type base HP, speed, damage, Sprite, Candy settings, ability radius/cooldown, and any future tuning. Ensure `VegetableType` (or existing stable enemy IDs) supports the new roster **without renumbering or replacing existing serialized enum entries or save identifiers**. Display names can change independently from persistent IDs.

## 3. Basic enemies

- **Carrot:** HP `100`, speed `2.0`, damage `100`.
- **Tomato:** HP `85`, speed `3.0`, damage `100`.
- **Eggplant:** HP `150`, speed `1.7`, damage `100`.

These variants require **no special abilities**. They spawn through the existing stage configuration, follow the existing 2D XY path, can be targeted/damaged/slowed, die exactly once, and apply their specified base damage only upon reaching the destination.

Do not confuse an enemy's `damage` stat with tower attack damage.

## 4. Garlic — speed support

**Specified:** Garlic HP `30`, speed `2`, damage `20`, `speedIncrease = 1.5`, and an area-based ally speed buff.

**Proposed interpretation:** treat `1.5` as a **1.5× movement-speed multiplier**, not an additive `+1.5` world-units/second increase; this is configurable and must be reported as an assumption.

Required behavior:

1. Garlic produces a circular support aura centered on its current world position, using a **configurable** `supportArea` radius in world units. No radius was provided; do not hard-code a claimed design value.
2. The aura affects **other living allied enemies** currently inside its area (exclude Garlic itself). The effect follows the moving Garlic.
3. Apply the buff immediately upon entering the area and remove it immediately upon exiting, Garlic dying/despawning, or the aura being disabled.
4. Use the current effective-speed/movement-modifier system. Do not overwrite base speed or interfere with persistent Slow Area effects. Recalculate effective speed when sources change.
5. Avoid repeatedly stacking the same Garlic instance's buff each frame. Track modifier sources. When multiple Garlic sources overlap, choose and document a deterministic policy; **recommended**: strongest applicable Garlic multiplier wins, rather than compounding by default.
6. No new Sprite or combat mechanic beyond this ability is required; integrate the correct sprite if it actually exists in the project.

Illustration (assumes no other modifiers): a base-speed `2.0` enemy under Garlic's `1.5×` aura moves at effective speed `3.0`.

## 5. Broccoli — shield support

**Specified:** Broccoli HP `200`, speed `1.8`, damage `30`, `shieldValue = 1000`, and a support area. Damage directed to allies inside the area is absorbed by its shield.

**Proposed initial interpretation** (to make the described ability deterministic while preserving the existing damage API):

1. Broccoli has **one shared, finite shield pool of `1000` HP per Broccoli instance**, not a new `1000`-HP shield for each protected ally. This pool is initialized on spawn/reset and is not replenished unless a future design explicitly requests regeneration.
2. It protects **other living allied enemies** within its configurable circular `supportArea`; by default, do not redirect damage dealt directly to Broccoli itself.
3. Before the protected enemy's health is reduced, the existing central damage pipeline offers the pending damage to a relevant Broccoli shield. Absorb up to remaining shield HP; if damage exceeds the remaining shield, pass the **unabsorbed remainder** through to the original enemy.
4. When shield reaches `0`, protection stops. Existing ally HP and Broccoli HP continue to work normally; do not make anyone permanently invulnerable.
5. If several Broccoli auras cover a target, handle them **deterministically** (for example, by stable runtime instance order), routing remaining damage through each finite pool at most once. No damage should disappear, duplicate, or recurse infinitely.
6. Apply to damage from Towers and the existing damaging skills through their **common enemy-damage API**. Existing slow/other status effects still apply normally; the shield blocks damage, not movement modifiers.
7. On Broccoli death/despawn, its pool ceases protecting allies. Clean up registration and prevent dangling references.
8. Provide a minimal shield-state visualization/debug indication if the project already supports it; elaborate shield VFX/UI are out of scope.

**Implementation note:** do not make every Tower and Skill individually check for Broccoli. Add the smallest safe interception/redirect point to the central enemy damage-resolution pipeline (or an existing damage-mitigation hook). Clearly document damage event accounting and kill attribution if the project depends on them. No Candy drop should be triggered from shield absorption alone.

## 6. Potato — disruptor (baseline choice)

**Specified:** Potato HP `150`, speed `1.8`, damage `20`; it throws dirt at the **nearest Tower**, disrupting that Tower.

**Proposed interpretation of `disrupts`:** temporarily **prevent the affected Tower from attacking**, without changing its placement, stats, range, ownership, or cards. Do not give the thrown dirt undocumented damage. Integrate with the Tower's existing attack-enabled/status system if available.

Required behavior:

1. Find the nearest **currently placed, valid Tower** by world-space XY distance when the ability activates.
2. Use configurable ability range, trigger/cooldown, and disruption duration; the source design does not specify numbers, so do not hard-code arbitrary gameplay balance values.
3. If no valid Tower is in range, do nothing and retry in accordance with the configured ability trigger/cooldown. Do not target placement slots or UI cards.
4. Apply a temporary attack-disable effect with **source-aware** cleanup. When the effect expires, the Tower can attack again unless another active disruption source still disables it.
5. If the Tower is removed or the Potato dies/despawns, clean up the relevant references and outstanding scheduled effects safely. Define and document whether a previously applied disruption persists until its own expiry or is removed on source death; **proposed:** it persists until its configured expiry.
6. Keep the dirt throw's presentation simple. A placeholder effect is acceptable; a full projectile/physics system is not required by this patch.
7. Do not stop an enemy from moving or a Tower from existing just because the attack system is disabled.

If the current Tower system has no reusable temporary disable mechanism, implement a minimal reversible `AttackDisabled`/status source abstraction, not a global blanket `enabled = false` that can break other Tower components.

### Radish — alternative only

The design also suggests Radish (`HP 120`, `speed 2.3`, `damage 20`) as a **replacement** disruptor that chooses a **random** Tower. This patch records that alternative but **does not enable or add Radish as a playable disruptor**. Ask for explicit approval before switching or adding it; do not create an extra card, EnemyDefinition, wave, or ability just because it is mentioned as an option.

## 7. Boss — data and deliberate limits

**Specified:** Boss HP `2500`, speed `1.2`; damage is written as **`tower health`**, with no numeric damage and no target/health-system semantics; the original description explicitly asks whether the boss should have a unique mechanic.

Implement the Boss as an identifiable enemy role/definition using the existing path/health/death/spawn pipeline. Do **not** invent a health value for Towers, interpret `tower health` as a numeric base-damage value, or silently make the Boss one-shot Towers/the Base.

- The Boss's **damage behavior is an explicit open decision**. If the current project already has Tower health and a clear API for direct Tower damage, inspect it and report a proposed mapping **without enabling unapproved destruction behavior**.
- If the project only supports damage to the Base at the path endpoint, keep the Boss endpoint damage **unconfigured/pending design confirmation** rather than inventing a number. Do not let an unset damage value cause a crash; use an explicit validation/reporting state.
- **No special Boss ability** is required in this patch. This first iteration can be a high-HP, slow Boss; unique mechanics can be specified in a separate follow-up patch.

A Boss definition may be excluded from release wave configurations until the unresolved damage behavior is supplied; it must not crash test spawns. Do not modify Tower HP or Base HP architecture solely for this incomplete design line.

## 8. Asset mapping and spawner compatibility

- Search the Unity project for existing sprites/definitions for Carrot, Tomato, Eggplant, Garlic, Broccoli, Potato, and Boss. Use correct matching art when available.
- Earlier vegetable sprites include **Tomato and Potato**, but Carrot/Eggplant/Garlic/Broccoli/Boss art was not established by the earlier asset list; do not guess or substitute mismatched images. Use an identifiable temporary placeholder only when required for an integration test, and report missing assets.
- Follow the project's established 2D pixel-art import settings: `Sprite (2D and UI)`, Point filtering where relevant, transparent alpha, consistent Pixels Per Unit and sorting.
- Preserve the refrigerator spawn point, kitchen path centerline, destination/Base damage flow, stage clear logic, and five-enemy repeatable test stage. Add a **separate development-only test stage or spawn harness** if necessary.
- Existing Veggie Enemy variants not in this role design must not be deleted or silently rebalanced.

## 9. Core integration constraints

Preserve without regression:

- Existing attacks and targeting, Toy Throw AoE and persistent Slow Area, including source-aware speed modifiers.
- Card selection toggle; placed-Tower range inspection; Tower placement and Wooden Plate slot presentation.
- Finite spawn/alive tracking, Victory/Retry/Pause, and Base reach behavior.
- Candy drop roll and automatic persistent currency award **exactly once per valid death**; do not pay Candy on shield absorption or on enemy reaching the Base.
- Gold clear rewards, deck/preset persistence, and stable identifiers.

Use 2D XY proximity queries, not 3D physics/XZ distances. The shield and aura effects must not cause duplicated death, stage-complete, or reward events.

## 10. Acceptance tests and edge cases

### Configuration

1. Carrot `100/2.0/100`, Tomato `85/3.0/100`, Eggplant `150/1.7/100`.
2. Garlic `30/2.0/20`, with `1.5` speed parameter; Broccoli `200/1.8/30`, with `1000` shield pool.
3. Potato `150/1.8/20` is the baseline disruptor; Radish remains an unelected alternative.
4. Boss `2500/1.2` exists without invented damage or special ability.

### Runtime

5. All configured role enemies travel the same existing waypoint path and participate in normal kill/escape/stage bookkeeping.
6. Garlic buff starts/stops at the correct configured radius; follows a moving Garlic; no repeated stacking from one source; overlapping buffs and Slow Area reconcile correctly.
7. Broccoli absorbs damage from an ally; a `1000`-HP pool hit by `600` then `500` absorbs `600 + 400`, with the last `100` reaching the original target. Verify no shield replenishment or damage recursion.
8. Broccoli does not apply its shield to its own HP under the proposed self-exclusion policy. When Broccoli dies, protected allies resume normal damage intake.
9. Potato selects the nearest valid Tower (not the nearest slot), disrupts attacks for the configured duration, and restores attacks afterward without breaking other active disable sources.
10. If no Tower is in range, Potato fails gracefully. Despawn/destroyed references do not cause exceptions.
11. Tower AoE, skill damage, slow, Candy and Victory continue to work. Existing repeatable five-enemy stage remains unchanged unless the user approves edits.
12. Missing art and unconfigured radii/cooldowns/Boss endpoint damage are reported as **not configured**, not presented as verified gameplay.

Use unit or EditMode tests for damage-routing/modifier bookkeeping where possible, and PlayMode tests or a reproducible test scene for 2D proximity and visual layering. Report exactly which tests actually ran.

## 11. Implementation order and reporting

1. Audit existing Enemy, EnemyDefinition, combat damage pipeline, movement modifiers, Tower attack control, spawners, assets, and save identifiers.
2. Add/adjust the three Basic definitions and the shared enemy role representation **without breaking previous variants**.
3. Implement Garlic's source-aware aura through the existing speed modifier system.
4. Implement Broccoli's finite shared shield interception through the central damage pipeline.
5. Implement Potato's nearest-Tower disruption using a reversible source-aware Tower status.
6. Add Boss data (HP/speed), clearly marking its damage and special mechanics unresolved.
7. Add a separate test harness and execute focused regression tests; avoid modifying release waves and the five-enemy test stage by default.

**Final implementation report must include:** exact changed files/definitions, Enemy-role architecture chosen, exact roster stats, Sprite availability/missing files, configured vs TBD ability values, multiplier/stacking/redirect policy used, Potato vs Radish decision, Boss open design questions, tests executed and their results, and anything not verified. Do not claim manual PlayMode tests were run unless they were.
