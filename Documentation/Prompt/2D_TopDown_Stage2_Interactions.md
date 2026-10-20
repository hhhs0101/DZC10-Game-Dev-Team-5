# 2D Top-Down Migration — Stage 2: Combat and World Interaction

## Objective

Reconnect existing **Tower placement, Enemy movement/targeting, Skill aiming and runtime Skill effects** to the native XY / Physics2D battlefield created in Stage 1.

Preserve gameplay rules, cooldowns, damage, costs, Hand cycling and Skill lifecycles. **Do not rebuild the Card, Currency or Stage domain systems.**

Prerequisite: Stage 1 acceptance gate has passed; read `2D_TopDown_Migration_Overview.md` and the current Skill/Tower specifications.

## 1. Audit Spatial Dependencies

Locate and trace all code that assumes the former 3D tabletop:

- `PlanarSpace` (if present) and XZ projection functions
- `WaypointPath` / `EnemyPathFollower`
- camera `ScreenPointToRay` / `Physics.Raycast` / `RaycastHit`
- `TabletopSurface` and 3D collider/layer use
- `TurretPlacementController` and slot lookups
- `SkillCastingController`, indicator and world-position conversion
- range/target selection using `Vector3.Distance` or XZ-only math
- `SkillAreaVisual`, persistent area triggers and slow-modifier cleanup
- Enemy/Tower prefab children, world-space HP bars, origin points

Update only world-facing portions. Prefer a clear XY spatial abstraction over adding more per-feature coordinate conversion hacks. Preserve existing pure gameplay logic.

## 2. Unified Mouse-to-World Conversion

Implement one authoritative conversion from screen mouse coordinates to the XY battlefield using the **orthographic camera and fixed gameplay Z plane**.

Conceptually:

```text
Mouse Screen Position
-> Camera screen-to-world conversion at gameplay Z plane
-> XY world point
-> PlayableArea2D containment query
```

Use actual camera/scene depth values correctly (`ScreenToWorldPoint` requires an appropriate depth), or use an equivalent plane-intersection technique. Do not confuse camera near clip distance with the world gameplay plane.

World pointing must be explicitly separated from UI pointer interaction using the project's `EventSystem` or equivalent. A mouse release over UI must not cast a Skill underneath the UI.

## 3. Physics2D Interaction

- Detect placement slots using appropriate `Collider2D` queries (`Physics2D.OverlapPoint`, a filtered 2D ray query, or an equivalent reliable method).
- Use explicit LayerMasks/filters as needed; distinguish `PlacementSlot`, `Enemy`, `PlayableArea` and non-interactive decoration.
- A table-background collider must not steal or mask valid slot selection.
- Do not use `Physics.Raycast`, `Collider`, `RaycastHit` or 3D trigger callbacks for the new 2D world objects.
- Keep UI hit testing separate from 2D world physics.
- Collider shape/size should match the gameplay target shown on screen.

## 4. Tower Placement Flow — Preserve Exact Contract

```text
Select Tower Card in Hand
-> Click available, valid PlacementSlot2D
-> Validate Card and slot
-> Validate affordability (including ElixirCost = 0)
-> Place selected Tower on configured slot placement point
-> Mark slot occupied
-> Spend cost exactly once
-> Rotate used Card exactly once
```

Failure cases (outside slots, occupied slot, insufficient Elixir, invalid selection):

- do not place Tower
- do not spend Elixir
- do not rotate Hand
- do not occupy slot

Use the existing validated placement/domain API where practical. A 2D placement point must be independent of the slot visual's center when configured differently. A stage can change slot count and XY locations without changing code.

## 5. Enemy Path Movement and End-of-Path

- Move actual Enemy runtime objects along XY waypoints at the same configured logical speed.
- Keep right-to-left start/end semantics in the initial map.
- Preserve spawn registration, death, base-reach reporting and stage alive-enemy tracking.
- Avoid counting base-reached/despawned Enemies as Candy-dropping kills.
- Keep movement speed modifier integration; use *effective* speed during path following.
- Do not introduce NavMesh, vertical collision gameplay, or new enemy categories.

## 6. Turret Targeting and Combat

- Compute gameplay distance on XY only (`Vector2.Distance` or squared XY distance).
- Preserve targeting strategies, range thresholds, attack interval, hit/damage API, health/death semantics.
- If current detection uses 3D overlap queries, switch the query to an appropriate Physics2D technique or reuse an authoritative Enemy registry with XY distance filtering.
- Decouple targeting from placeholder visuals and SpriteRenderer scale.
- Keep future 2D projectile origins configurable without implementing new projectile behavior now.

## 7. Skill Selection, Hold-to-Aim, Release-to-Cast

Preserve the current control contract:

```text
Select Skill Card
-> LMB Down on valid playable area
-> Enter aiming mode
-> Hold LMB: indicator follows XY mouse position
-> LMB Release:
   valid playable area -> attempt cast
   originating Skill Card -> cancel
   other UI -> cancel
   outside playable area -> reject/cancel without cost
```

Aiming/cancellation must work even if the pointer moves between the world and screen-space UI. Do not cast on card selection alone.

The target world position and radius use the same gameplay plane and configured values as the actual effect.

## 8. One Source of Truth for Radius

For each circular Skill:

```text
SkillDefinition.Radius (world units)
├── 2D aim preview radius
├── Toy Throw damage query radius
├── Persistent Slow Area zone radius
└── active-area visual radius
```

Compensate properly for sprite pixels-per-unit, transform scaling and collider shape so **the visible radius and gameplay radius coincide in world units**. Avoid independent inspector radii for physics and visuals.

## 9. Toy Throw

- Preserve its existing AoE damage and configured cast rules.
- Use the XY target point and a 2D circle query / Enemy registry filter to find enemies in range.
- Damage all valid enemies inside the configured radius; do not damage those outside.
- Preserve future `CastingTime` and `TravelTime` distinction and optional projectile origin.
- Do not introduce new projectile animations or change gameplay delivery timing as part of the 2D migration.

## 10. Persistent Slow Area

- Spawn a flat 2D zone visual at the XY cast location.
- Use `Collider2D` triggers and/or authoritative XY membership reconciliation, as appropriate for the existing design.
- Configurable area radius, slow multiplier, active duration (8 seconds by default), and visual-only fade.
- Slowing applies **only while an Enemy is within an active zone**.
- Immediately remove that zone's modifier when the Enemy leaves its radius.
- When the zone expires, remove its modifier from every affected Enemy at once; fade the visual only afterward.
- Each zone tracks independent sources; identical slow zones do not multiply slow strength under the existing rule.
- Preserve base speed and effective movement-speed modifier logic.
- Clean stale references and modifiers on Enemy death, teleport/disable/despawn, pause/reset and zone destruction. Do not rely solely on `OnTriggerExit2D` if it may be skipped by disabling/destroying an object.

If membership is computed geometrically rather than by triggers, compare XY coordinates using the same zone radius. For trigger-based membership, verify entry, exit and cleanup semantics experimentally.

## 11. Portable Presentation

- Replace placeholder visuals with `SpriteRenderer`/2D UI as needed, without moving runtime rules onto SpriteRenderer objects.
- Preserve readable Enemy HP bars, placement highlights and skill-zone visuals.
- Use 2D sorting order to prevent paths/areas from obstructing click targets or important units.
- Keep presentational helpers detachable and configurable.

## 12. Scope Exclusions

Do not implement during Stage 2:

- new card types or new effects
- economy/progression rewrites
- upgrade shop, Tower merge or synergy
- final art, animation, or projectile mechanics
- deletion of existing 3D scene
- switching the main game entry to 2D before Stage 3 succeeds

## 13. Acceptance Gate

Verify in the **2D scene**:

1. Hand Tower Card selection then click on available slot places the right Tower.
2. Click outside slots, on occupied slots, or with insufficient resources rejects correctly.
3. Valid placement spends cost and cycles the Card once; invalid placement never does.
4. Zero-cost Tower Cards work.
5. Enemies follow XY path and move at configured (including slowed) speeds.
6. Towers target and damage only valid enemies in configured XY range; attack intervals remain intact.
7. Skill LMB press/hold/release/cancel flow matches current gameplay behavior.
8. Aim preview radius and effect radius agree for multiple configurable radius values.
9. Toy Throw damages all and only enemies in radius.
10. Slow Area slows enemies while inside, clears immediately on exit and on expiry, and fades its visual after gameplay expiration.
11. Two overlapping identical Slow Areas do not stack multiplicatively and do not remove each other's effect.
12. Despawn/death/reset does not leave permanent slow modifiers or stale references.
13. Existing Card cost, Hand cycling, target policy and damage rules remain unchanged.
14. No new world input still accidentally targets the old 3D physics scene.

Run focused automated tests where practical, and separately report which manual mouse/Play Mode scenarios were actually exercised. **Do not proceed to Stage 3 on failed placement or Skill parity.**
