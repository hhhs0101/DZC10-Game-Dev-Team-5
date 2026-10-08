# 3D Skill Interaction Specification

## Purpose

Stabilize and extend the existing Skill Card system after the 2D-to-3D battlefield migration.

Preserve the existing Skill interaction model conceptually:

```text
Select Skill Card
-> Hold Left Mouse Button
-> Show Skill area indicator
-> Indicator follows mouse
-> Release Left Mouse Button
-> Cast or Cancel
```

The primary migration is from 2D world targeting to the 3D tabletop battlefield.

Read and preserve the architecture defined in the existing project specifications, especially:

- `Main.md`
- `Tower.md`
- `enemy.md`
- `CardSystem.md`
- `Elixir.md`
- `Skill.md`
- `HandCycle.md`
- `3D_Implementation_Stage1.md`
- `3D_Stage1_Placement_Camera_Patch.md`

Do not rewrite working systems unless integration requires it.

---

## 1. Core Design Principle

Keep:

```text
Visual World = 3D
Gameplay Plane = Tabletop XZ Plane
Y Axis = Presentation Only
```

Skill targeting and effects should remain planar even though they are represented in 3D.

Do not introduce height-dependent Skill behavior yet.

---

## 2. 3D Skill Aiming

Required flow:

```text
Select Skill Card
-> LMB Down on valid gameplay area
-> Enter Aiming Mode
-> Show circular indicator
-> While LMB is held, indicator follows mouse
-> LMB Release:
   - valid tabletop position -> Attempt Cast
   - originating Skill Card -> Cancel
   - other UI -> Cancel
```

Selecting the Skill Card alone must not cast it.

---

## 3. Mouse-to-World Conversion

Use the fixed gameplay Camera and 3D Physics:

```text
Mouse Position
-> Camera.ScreenPointToRay(...)
-> Physics.Raycast(...)
-> GameplaySurface / Tabletop hit point
```

Use an appropriate LayerMask.

Do not use old `Physics2D` or 2D XY assumptions for 3D Skill targeting.

Decorative objects, Towers, or Enemies should not incorrectly change the intended tabletop target point.

---

## 4. One Radius Source of Truth

The visible targeting area and actual gameplay effect area must use the same configured value:

```text
SkillDefinition.Radius
├── Aiming Indicator Radius
├── Runtime Effect Radius
└── Trigger / Query Radius
```

Do not maintain separate hard-coded visual and gameplay radii.

Changing the configured radius must update both visuals and actual effect range.

---

## 5. Portable Skill Visuals

Keep Skill visuals loosely coupled from gameplay logic.

Preferred structure:

```text
Skill Runtime / Effect
    -> gameplay state

Skill Visual
    -> observes runtime state
    -> renders indicator / effect
```

Placeholder visuals should be easy to disable, replace, or reconnect later.

---

# Toy Throw

## 6. Replace Fireball with Toy Throw

Replace the current Fireball concept with:

```text
Toy Throw
```

Its current gameplay role remains an Area Damage Skill.

For the current implementation:

- Circular AoE
- Configurable Damage
- Configurable Radius
- Damages every valid Enemy inside the radius
- Uses existing Card, Hand, Elixir, and success/cancel rules

---

## 7. Current Toy Throw Behavior

For this implementation, Toy Throw may apply damage immediately after a successful cast:

```text
Target selected
-> Validate cast
-> Validate Elixir
-> Commit Skill
-> Spend Elixir
-> Cycle Card
-> Apply AoE damage
```

Do not require final toy assets or projectile animation yet.

---

## 8. Future Projectile Compatibility

Prepare Toy Throw so a future projectile visual can be added without redesigning the Card/Skill systems.

Future intended flow:

```text
Cast committed
-> optional CastingTime
-> Toy spawned from ThrowOrigin
-> Toy travels toward TargetPosition
-> Toy reaches target
-> AoE damage occurs
```

Keep these concepts separate:

```text
CastingTime
= delay between cast commitment and projectile launch

TravelTime
= time between projectile launch and arrival
```

Do not use a single variable for both.

For the current prototype, it is acceptable to use:

```text
CastingTime = 0
TravelTime = 0
```

A future version may use, for example:

```text
CastingTime = 0
TravelTime = 0.7
```

without changing the Card system.

---

## 9. Future Throw Origin

Keep the architecture compatible with an optional projectile origin:

```text
SkillCastContext
├── TargetPosition
└── Optional ThrowOrigin
```

The origin may later be:

- an off-screen child character
- a fixed world point
- an animated throw source

Do not hard-code projectile-origin logic into damage calculation.

---

# Persistent Slow Area

## 10. Replace Arrow Rain Damage with a Persistent Slow Area

Replace the current Arrow Rain direct-damage behavior with a persistent Slow Area.

The Skill may keep a temporary name for development. It may later be themed as something such as `Milk Spill`.

Required behavior:

- Circular targeting remains
- Direct Damage = 0
- Creates a persistent area at the selected tabletop position
- Slows Enemies while they are inside
- Removes that Slow immediately when they leave
- Area lasts for a configurable duration
- Area remains visually visible while active
- Visual fades out after gameplay effect ends

---

## 11. Area Duration

Use configurable values.

Initial gameplay duration:

```text
ActiveDuration = 8.0 seconds
```

Use a separate visual fade duration, for example:

```text
FadeDuration = 0.5 seconds
```

The fade duration is presentation-only and may be adjusted independently.

---

## 12. Slow Area Lifecycle

Required lifecycle:

```text
Successful cast
-> Create Slow Area
-> Area active

Enemy enters
-> Add this Area as a Slow source

Enemy remains inside
-> Slow remains active

Enemy exits
-> Remove this Area's Slow source immediately

ActiveDuration expires
-> Stop applying gameplay Slow immediately
-> Remove this Area's Slow source from all affected Enemies
-> Begin visual fade-out

FadeDuration expires
-> Remove / despawn Area visual/runtime object
```

The gameplay effect must end at `ActiveDuration`, not after the fade.

---

## 13. Zone-Based Slow, Not Timed Enemy Debuff

This effect is an active zone condition.

Correct rule:

```text
Enemy inside active Slow Area
-> Slowed

Enemy outside active Slow Area
-> Not slowed by that Area
```

Do not apply an independent 8-second Slow timer to each Enemy.

If an Enemy exits after 2 seconds, that Area's Slow must end immediately for that Enemy.

---

## 14. Enemy Movement Modifier Architecture

Do not overwrite base movement speed directly and restore it manually.

Avoid:

```text
enemy.MovementSpeed = slowedValue
...
enemy.MovementSpeed = originalValue
```

Use or introduce a modifier system:

```text
BaseMovementSpeed
+ Active Movement Modifiers
-> EffectiveMovementSpeed
```

Example:

```text
Base Speed = 4.0
SlowMultiplier = 0.5
Effective Speed = 2.0
```

Keep base speed unchanged.

This should remain extensible for future:

- Freeze
- Speed Buff
- Rage
- other movement effects

Do not implement those effects now.

---

## 15. Source-Aware Slow Tracking

Each Slow Area is an independent effect source.

Example:

```text
Enemy affected by:
Area A
Area B
```

If the Enemy exits Area A but remains in Area B:

```text
Remove Area A source
Keep Area B source
Enemy remains slowed
```

Do not use one global `isSlowed` boolean for all sources.

---

## 16. Slow Overlap Rule

For the current prototype, identical Slow Areas should not multiply their Slow strength.

Example:

```text
Area A = 0.5x
Area B = 0.5x

Enemy inside both
-> movement remains 0.5x
not 0.25x
```

However, track both sources independently so the Enemy remains slowed until no active source remains.

Keep the modifier system extensible for future stacking rules.

---

## 17. Configurable Slow Strength

Use a configurable value such as:

```text
SlowMultiplier = 0.5
```

This means 50% normal movement speed.

Do not hard-code the final balance value into Enemy code.

---

## 18. 3D Slow Area Representation

Use a simple replaceable 3D tabletop representation such as:

- flat circular mesh
- transparent disk
- simple projected circle

Recommended structure:

```text
PersistentSlowArea
├── Runtime Effect Controller
├── Trigger Collider
└── Visual
    └── Flat placeholder area
```

A thin 3D trigger volume may be used to detect Enemy enter/exit.

The trigger may have enough height to overlap current ground Enemy Colliders reliably even though gameplay remains planar.

The visual mesh must not be the authoritative gameplay state.

---

## 19. Radius Consistency for the Slow Area

Use the same Skill radius for:

```text
SkillDefinition.Radius
├── Aiming Indicator
├── Persistent Area Visual
└── Trigger Collider
```

Do not independently scale them.

---

## 20. Enemy Destruction and Cleanup Safety

If an Enemy dies while inside the Area:

- clean effect tracking safely
- do not leave stale Enemy references
- Area expiry must not throw errors later

If the Area expires while Enemies remain inside:

- remove that Area's modifier source from every affected Enemy safely

---

# Shared Card / Elixir Rules

## 21. Successful Skill Use

Both Skills preserve the existing card-use contract.

Successful cast:

```text
Valid target
AND
sufficient Elixir
-> Commit Skill
-> Spend Elixir
-> Cycle Card
```

---

## 22. Cancelled / Failed Skill Use

Cancelled or invalid Skill use:

```text
No Elixir spent
No Skill effect
No Card cycle
Skill remains in Hand
```

Insufficient Elixir must continue using the existing insufficient-Elixir feedback.

Do not create separate Hand-cycle rules for Skills.

---

## 23. Cancellation Behavior

Preserve the existing cancellation interaction:

```text
Select Skill
-> Hold LMB
-> Aim
-> Move cursor back over originating Skill Card
-> Release LMB
-> Cancel
```

Also cancel if LMB is released over other UI.

Cancellation must:

- hide aiming indicator
- spend no Elixir
- create no effect
- keep the Card in Hand
- leave RuntimeCardCycle unchanged

---

# Architecture

## 24. Skill Runtime Structure

The Skills now represent different runtime categories:

```text
Skill Runtime
├── Area Damage behavior
│   └── Toy Throw
└── Persistent Area behavior
    └── Slow Area
```

Prefer reusable behavior separation over copying the full cast pipeline into each Skill subclass.

Possible architecture:

```text
SkillRuntime
├── AreaDamageSkillRuntime
└── PersistentAreaSkillRuntime
```

or a composition-based equivalent.

Use OOP where there is a clear relationship, but do not over-engineer the hierarchy.

---

## 25. Current Scope

### Implement Now

- 3D tabletop Skill aiming
- Camera Ray -> GameplaySurface targeting
- Radius-consistent aiming indicator
- Toy Throw replacing Fireball concept
- Toy Throw AoE damage
- separate CastingTime and TravelTime concepts
- Persistent Slow Area replacing Arrow Rain damage
- configurable Slow radius
- configurable SlowMultiplier
- configurable 8-second ActiveDuration
- configurable FadeDuration
- visible persistent area
- fade-out after gameplay effect expires
- immediate Slow removal on Enemy exit
- overlapping source-aware Slow handling
- reusable Enemy movement modifiers

### Do Not Implement Yet

- final Toy model
- projectile flight animation
- child throwing animation
- final Milk Spill visual
- fluid simulation
- advanced shaders
- status-effect UI icons
- Flying Enemy Skill interaction
- height-dependent Skill behavior
- Skill upgrades
- final VFX/audio

---

## 26. Verification

Verify at minimum:

### 3D Aiming

1. Selecting a Skill Card does not cast immediately.
2. LMB Down on tabletop enters Aiming Mode.
3. Indicator follows the mouse using 3D tabletop raycasting.
4. Indicator remains on the tabletop.
5. Release on valid tabletop attempts cast.
6. Release over originating Card cancels.
7. Release over other UI cancels.
8. Visual radius matches actual effect radius.

### Toy Throw

9. Toy Throw replaces Fireball content.
10. Toy Throw damages all valid Enemies inside its radius.
11. Enemies outside radius are unaffected.
12. Existing Elixir/Card-cycle rules are preserved.
13. CastingTime and TravelTime are separate concepts.
14. Current Toy Throw works without projectile visuals.

### Persistent Slow Area

15. Area appears at cast location.
16. Area deals no direct damage.
17. Enemy entering becomes slowed.
18. Enemy leaving immediately loses that Area's Slow.
19. Enemy remaining inside stays slowed.
20. ActiveDuration is configurable and initially 8 seconds.
21. Slow stops immediately when ActiveDuration expires.
22. Visual fade happens after gameplay effect ends.
23. Area object is removed after fade.
24. Slow strength is configurable.
25. Base Enemy movement speed is not permanently modified.

### Overlap / Safety

26. Two identical Slow Areas do not multiply Slow strength.
27. Leaving one Area while remaining in another keeps Enemy slowed.
28. Leaving all Areas restores normal effective speed.
29. Expiring one Area does not remove another Area's active source.
30. Enemy death inside Area does not create stale-reference errors.

### Existing Systems

31. Insufficient Elixir creates no effect and no cycle.
32. Cancelled cast spends no Elixir and does not cycle.
33. Existing Tower placement remains functional.
34. Existing Hand / Deck / RuntimeCardCycle behavior remains intact.
35. Pause behavior remains consistent with the existing game-state system.

---

## 27. Implementation Report

After implementation, report:

1. Existing Skill classes/components reused.
2. Files created or modified.
3. 2D-to-3D aiming changes.
4. Mouse-to-tabletop raycast implementation.
5. How one Radius value drives visuals and gameplay.
6. Toy Throw runtime architecture.
7. CastingTime vs TravelTime design.
8. Persistent Slow Area architecture.
9. Enemy movement-modifier architecture.
10. Slow overlap/source-tracking behavior.
11. Configured development values for:
    - Toy Throw Damage
    - Toy Throw Radius
    - Toy Throw CastingTime
    - Toy Throw TravelTime
    - Slow Area Radius
    - SlowMultiplier
    - ActiveDuration
    - FadeDuration
12. Automated tests performed.
13. Manual mouse/visual tests actually performed.
14. Anything that remains unverified.

Do not claim manual Skill aiming or visual verification unless it was actually tested.
