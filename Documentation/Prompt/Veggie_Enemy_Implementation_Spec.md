# Veggie Enemy Variants — 2D Top-Down Implementation Specification

## Purpose

Create ten visually distinct vegetable-themed enemy variants for the current 2D top-down tower-defense battlefield. Reuse the project's existing Enemy architecture and gameplay systems. This is primarily a **data and visual-content integration task**, not a rewrite of enemy combat logic.

## 1. Source Assets

The preferred inputs are the individually exported PNG sprites supplied with the asset pack:

| Variant | Expected sprite file |
| --- | --- |
| Mushroom | `Mushroom.png` |
| Olive | `Olive.png` |
| Green Cabbage | `GreenCabbage.png` |
| Zucchini | `Zucchini.png` |
| Tomato | `Tomato.png` |
| Kohlrabi | `Kohlrabi.png` |
| Turnip | `Turnip.png` |
| Leek | `Leek.png` |
| Red Cabbage | `RedCabbage.png` |
| Potato | `Potato.png` |

Prefer these separate PNGs over manually slicing the sheet because each variant needs a separately assignable Sprite. Search the actual Unity project for the files and use the paths found; do not assume they are already imported.

The reference sprite sheet, `VEGGIES_LINE.png`, is **160 × 192 pixels**, arranged as **five columns by six rows of 32 × 32 pixel cells**. It may be used as an alternative source only if necessary. The sheet contains additional vegetables, so do **not** guess which cell corresponds to which requested variant. Verify every visual mapping before assigning any sprite. If the individual PNGs are missing, report the missing files rather than silently substituting unrelated images.

## 2. Core Architecture

**Do not create ten independent Enemy MonoBehaviour subclasses solely to assign sprites.** The ten variants should reuse the current Enemy runtime behavior and differ through data/configuration.

Preferred conceptual model:

```text
EnemyDefinition (or existing equivalent)
├── Stable Enemy Type ID
├── Display Name
├── Sprite / Visual Definition
├── Health and Movement Data
├── Existing combat attributes
└── CandyDropChance / CandyDropAmount (if supported)
         │
         ▼
Shared Enemy Runtime / Base Enemy
├── Path-following
├── Health and Damage
├── Slow / Movement Modifiers
├── Death Events
├── Stage Alive-Enemy Tracking
└── Visual
    └── SpriteRenderer
```

Reuse existing `EnemyDefinition`, Enemy inheritance, factories, spawners, and prefab architecture where appropriate. If the project already uses a data-driven prefab or variant architecture, follow it rather than adding a duplicate system.

## 3. Ten Enemy Variants

Create or configure one identifiable enemy data asset per listed vegetable. Each variant must resolve to the correct source Sprite and be independently selectable by the existing enemy-spawn configuration.

Use stable, unique enemy type IDs, following the project's existing identity conventions. Example values (only if no convention exists):

```text
enemy_mushroom
enemy_olive
enemy_green_cabbage
enemy_zucchini
enemy_tomato
enemy_kohlrabi
enemy_turnip
enemy_leek
enemy_red_cabbage
enemy_potato
```

Do not assign invented type-specific combat abilities or arbitrary balancing differences. For the first pass, the ten variants may share the current basic enemy's gameplay statistics. Health, speed, damage and Candy drop configuration should remain independently editable for future balancing.

## 4. Import Settings for Pixel Art

For each source PNG, configure appropriate Unity 2D sprite import settings:

- Texture Type: `Sprite (2D and UI)`.
- Sprite Mode: `Single` for individual PNGs.
- Filter Mode: `Point (no filter)` to preserve crisp pixel art.
- Texture compression: preferably `None` if compression introduces artifacts.
- Mipmaps: disabled unless project-specific rendering actually needs them.
- Alpha/transparency: preserve the supplied artwork's transparent background.
- Pixels Per Unit: consistent across the ten sprites and appropriate to the existing scene's unit scale.

Do not automatically rescale or crop artwork in a way that alters the intended pixels. Keep displayed sizes consistent using a shared visual footprint where practical. Any optional per-variant sprite offset or scale should be visual configuration only and must not alter the gameplay range or collision rules.

## 5. Shared Runtime Prefab and Presentation

Prefer a reusable Enemy prefab whose runtime behavior remains independent from the vegetable Sprite:

```text
Enemy (runtime root)
├── Existing Enemy scripts / definition binding
├── 2D collider / physics components already required by combat
├── HP bar / status feedback (if existing)
└── Visual
    └── SpriteRenderer (vegetable sprite)
```

A prefab variant per vegetable is acceptable **only** if it is already the project's established method and avoids duplicated logic. Do not manually maintain ten diverging sets of movement/combat scripts.

Requirements:

- Face the sprites in the existing top-down XY battlefield orientation.
- Use consistent SpriteRenderer sorting layers/orders.
- Keep the sprite fully visible over the path/background.
- Preserve existing HP bars and status visuals.
- Do not accidentally tint sprites through shared materials or inherited renderer colors.
- Keep sprite replacement independent from enemy game logic.

## 6. Existing Gameplay Behavior Must Remain Intact

Every vegetable type must work through the same systems used by existing enemies:

1. Spawn from the existing Stage/Spawner configuration.
2. Follow the configured 2D XY waypoint path.
3. Be targeted by existing Tower targeting strategies.
4. Take damage through existing Enemy damage APIs.
5. Be affected by Toy Throw area damage.
6. Receive the persistent Slow Area modifier while within its gameplay radius.
7. Immediately lose a Slow Area's modifier on exit or expiry, without removing other valid modifiers.
8. Die and report death exactly once.
9. Participate correctly in Stage alive-enemy tracking and Victory checks.
10. Use the existing Candy drop pipeline, with per-definition drop settings.
11. Interact with base-reach / escape handling according to current rules.

Do not introduce new movement, attack, or death logic purely for the new visuals.

## 7. Collider and Slow-Area Compatibility

Use the existing **2D** collider architecture for the 2D battlefield. Do not add `BoxCollider`/`SphereCollider` or other 3D physics components to new 2D enemy variants.

Keep gameplay collision bounds deliberate and reasonably consistent between variants. In particular, avoid deriving the hitbox blindly from transparent sprite canvas margins. Ensure any `Rigidbody2D`/`Collider2D` setup needed for the existing 2D trigger-based Skill system remains valid.

A different visual footprint must not unintentionally change a tower's configured targeting range or a skill's configured AoE radius.

## 8. Stage / Spawner Integration

Make all ten enemy definitions available to the project's existing Stage/Spawner configuration. A stage should be able to select which types appear without code changes.

Do **not** replace or break the existing repeatable five-enemy Test Stage, its Victory behavior, or its fixed Gold reward. If additional visual verification is useful, add a separate development-only way to spawn one of each vegetable type, or use an existing spawn-testing mechanism. Do not change the five-enemy stage into a ten-enemy stage without explicit authorization.

## 9. Asset and Data Maintenance

- Keep assets organized under the existing project structure.
- Preserve existing Unity `.meta` identities and references where possible.
- Avoid changing unrelated Card IDs, Deck Presets, Stage IDs, or persistent save formats.
- Do not replace working existing enemies unless the new variants are explicitly intended to supersede their visuals.
- Do not add animation, new abilities, attack effects or elaborate visual polish yet.

## 10. Verification and Acceptance Criteria

Verify, where possible:

1. The ten PNGs are found in the project and import as Sprites.
2. Each named variant displays the **correct** vegetable sprite, not an arbitrary sheet cell.
3. There are ten independently addressable enemy data variants.
4. The runtime Enemy behavior is shared/reused rather than duplicated ten times.
5. All ten variants can spawn through the existing spawner.
6. Their sprites render crisply with proper transparency and correct sorting.
7. Each enemy follows the configured 2D path.
8. Existing Tower attacks can target and damage each type.
9. Toy Throw affects each type according to the existing radius/damage rules.
10. Persistent Slow Area applies/removes effects correctly for each type.
11. Each type's death is counted only once.
12. Candy drop settings remain per enemy definition and work through the existing reward system.
13. Existing Stage Clear and Replay behavior remain intact.
14. The five-enemy Test Stage remains finite and repeatable.
15. Existing Deck/Hand/Elixir/Gold/Candy persistence behavior is unchanged.

Do not claim Play Mode, manual rendering, or sprite-to-name matching was verified unless it actually was.

## 11. Implementation Report

Report:

1. Existing Enemy runtime/data/prefab architecture discovered.
2. All ten variant names, asset paths and definition paths.
3. Any missing sprite files or ambiguous mappings.
4. Import settings and shared scale/sorting policy.
5. Prefab/data changes made.
6. Spawner integration method.
7. Existing systems reused unchanged.
8. Automated tests executed.
9. Play Mode/manual checks actually performed.
10. Remaining limitations or unverified behavior.
