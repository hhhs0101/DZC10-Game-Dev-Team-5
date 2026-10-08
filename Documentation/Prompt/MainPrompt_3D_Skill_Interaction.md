# Main Prompt — Implement 3D Skill Interaction Stabilization

Read the existing project specifications before changing the project:

- `Main.md`
- `Tower.md`
- `enemy.md`
- `CardSystem.md`
- `Elixir.md`
- `Skill.md`
- `HandCycle.md`
- `3D_Implementation_Stage1.md`
- `3D_Stage1_Placement_Camera_Patch.md`

Then read and implement:

- `3D_Skill_Interaction_Spec.md`

Treat these specification files as the source of truth.

---

## 1. Inspect Before Editing

Before changing code, inspect the current Unity project and identify the existing classes/components responsible for:

- Skill Card definitions
- Fireball / Arrow Rain behavior
- Skill selection
- Skill aiming indicator
- mouse-to-world conversion
- Elixir validation
- Card-use success / failure / cancellation
- Enemy damage
- Enemy movement speed
- RuntimeCardCycle
- Pause / Game Over state
- 3D GameplaySurface / Tabletop Layer and Collider

Reuse working architecture where possible.

Do not replace existing systems simply because a new implementation would be easier.

---

## 2. Main Implementation Goals

Implement the following changes:

1. Preserve the existing 2D Skill interaction model conceptually.
2. Migrate Skill aiming to the 3D tabletop.
3. Use Camera Ray + `Physics.Raycast` against `GameplaySurface`.
4. Ensure one configured Skill Radius drives both visual and actual effect range.
5. Replace Fireball content with `Toy Throw`.
6. Keep Toy Throw as AoE damage for the current implementation.
7. Keep `CastingTime` and future projectile `TravelTime` as separate concepts.
8. Do not require projectile visuals yet.
9. Replace Arrow Rain direct damage with a persistent Slow Area.
10. Keep the Slow Area active for a configurable duration, initially 8 seconds.
11. Slow Enemies only while they remain inside an active Slow Area.
12. Remove the Slow immediately when an Enemy exits.
13. Stop gameplay Slow immediately when the Area expires, then fade the visual.
14. Use or introduce source-aware movement modifiers instead of overwriting Enemy base speed.
15. Ensure overlapping identical Slow Areas do not multiply Slow strength.
16. Preserve Card, Hand, Elixir, Tower, Enemy, Lobby, Pause, and Retry behavior.

---

## 3. Required 3D Aiming Flow

Use:

```text
Select Skill Card
-> LMB Down on tabletop
-> Enter Aiming Mode
-> Show circular indicator
-> Indicator follows cursor
-> LMB Release
```

Release behavior:

```text
Valid GameplaySurface
-> Attempt Cast

Originating Skill Card
-> Cancel

Other UI
-> Cancel
```

Use:

```text
Mouse Position
-> Camera.ScreenPointToRay(...)
-> Physics.Raycast(...)
-> GameplaySurface hit point
```

Do not use obsolete 2D world-position assumptions.

---

## 4. Radius Requirement

There must be one source of truth:

```text
SkillDefinition.Radius
```

It must drive all relevant representations:

```text
Aiming Indicator
Toy Throw AoE
Slow Area visual
Slow Area trigger/effect radius
```

Do not create separate manually synchronized radius values.

---

## 5. Toy Throw

Current implementation:

```text
Toy Throw
- circular Area Damage
- configurable Damage
- configurable Radius
- configurable CastingTime
- configurable TravelTime
```

For now:

- The Skill may apply damage immediately after successful cast.
- Projectile graphics are not required.
- Final toy assets are not required.

Preserve future compatibility with:

```text
Commit cast
-> wait CastingTime
-> spawn Toy at ThrowOrigin
-> travel for TravelTime
-> reach TargetPosition
-> apply AoE damage
```

Do not treat `TravelTime` as `CastingTime`.

---

## 6. Persistent Slow Area

Replace Arrow Rain's current damage effect with a persistent Slow Area.

Required behavior:

```text
Successful cast
-> Create Slow Area

Enemy enters
-> add this Area as a Slow source

Enemy remains inside
-> keep Slow active

Enemy exits
-> remove this Area's Slow source immediately

Area ActiveDuration expires
-> stop gameplay effect immediately
-> remove this Area's source from affected Enemies
-> fade visual

Fade completes
-> destroy/despawn Area
```

Initial configuration:

```text
ActiveDuration = 8 seconds
```

Keep `ActiveDuration`, `FadeDuration`, `Radius`, and `SlowMultiplier` configurable.

---

## 7. Movement Modifier Requirement

Do not implement Slow by overwriting and restoring the Enemy's base speed.

Use or introduce a model conceptually like:

```text
BaseMovementSpeed
+ Active Movement Modifiers
-> EffectiveMovementSpeed
```

Track Slow Areas as independent effect sources.

For identical Slow Areas:

```text
one or more active identical Slow sources
-> apply configured Slow strength once
```

Example:

```text
0.5x Area A
0.5x Area B
Enemy inside both
-> 0.5x movement, not 0.25x
```

But both sources must remain tracked independently.

Leaving Area A while remaining in B must keep the Enemy slowed.

---

## 8. Card and Elixir Contract

Successful Skill cast:

```text
Valid target
AND
sufficient Elixir
-> Commit Skill
-> Spend Elixir
-> Cycle Card
```

Cancelled, invalid, or unaffordable use:

```text
No Elixir spent
No Skill effect
No Card cycle
Card remains in Hand
```

Reuse the existing insufficient-Elixir feedback.

Do not create a second Skill-specific card-cycle system.

---

## 9. Scope Control

Do not add unrelated features.

Do not implement yet:

- final Toy projectile model
- projectile flight animation
- child throwing animation
- final Milk Spill art
- fluid simulation
- advanced shaders
- status-effect HUD
- Flying Enemy interaction
- Y-axis Skill mechanics
- Skill upgrades
- final VFX/audio

Focus on stable 3D Skill interaction and runtime behavior.

---

## 10. Testing

Run existing relevant tests and add focused tests where practical.

Verify at minimum:

- 3D mouse-to-tabletop targeting
- aiming indicator and gameplay radius consistency
- Toy Throw AoE damage
- Skill cancellation
- insufficient-Elixir rejection
- successful Skill card cycling
- Slow Area enter/exit behavior
- Slow removal immediately after Enemy exits
- Area expiration
- visual fade after gameplay expiration
- source-aware overlap handling
- no multiplicative stacking of identical Slow Areas
- Enemy death while inside Slow Area
- existing Tower placement remains functional
- existing Hand/Deck/RuntimeCardCycle remains functional
- Pause interaction remains consistent

Do not claim manual mouse or visual behavior was tested unless it was actually tested.

---

## 11. Final Report

After implementation, report:

1. Existing classes/components reused.
2. Files created or modified.
3. Skill aiming migration from 2D to 3D.
4. Tabletop raycast implementation.
5. Radius source-of-truth implementation.
6. Toy Throw implementation.
7. `CastingTime` vs `TravelTime` architecture.
8. Persistent Slow Area implementation.
9. Enemy movement-modifier architecture.
10. Slow source-tracking and overlap behavior.
11. Final configured development values.
12. Automated tests performed.
13. Manual tests actually performed.
14. Any unverified behavior or known limitations.
