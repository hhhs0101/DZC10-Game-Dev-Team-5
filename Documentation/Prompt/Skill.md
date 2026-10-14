# Skill Card and Skill Runtime System

## Purpose

Add Skill Cards as a second Card category alongside Tower Cards.

Skill Cards must use a separate runtime Skill architecture from the existing Tower/Turret system while sharing the common Card, Deck, Hand, and Elixir systems.

Implement two prototype Skills:

- Fireball
- Arrow Rain

Use inheritance and composition where appropriate.

## 1. Skill Architecture

Recommended conceptual structure:

```text
SkillDefinition
├── FireballSkillDefinition
├── ArrowRainSkillDefinition
└── FutureSkillDefinition...
```

Runtime behavior may follow:

```text
Skill
├── FireballSkill
├── ArrowRainSkill
└── FutureSkill...
```

The exact division between ScriptableObject data and runtime behavior may follow the existing project architecture.

Card-level structure remains:

```text
CardDefinition
├── TowerCardDefinition
└── SkillCardDefinition
```

A `SkillCardDefinition` references a `SkillDefinition`.

Do not make Skill runtime classes inherit from Turret runtime classes.

## 2. Common Skill Data

Support common Skill properties such as:

- Damage
- Radius
- Casting Time
- Targeting/cast rules
- Other future effect parameters

For the current prototype:

```text
Fireball:
- smaller radius
- higher damage
- CastingTime = 0

Arrow Rain:
- larger radius
- lower damage
- CastingTime = 0
```

Keep values configurable rather than hard-coded into UI logic.

## 3. Fireball

Implement Fireball as:

- Circular AoE
- Smaller radius
- Higher instant damage
- Damages every valid Enemy inside the radius

## 4. Arrow Rain

Implement Arrow Rain as:

- Circular AoE
- Larger radius
- Lower instant damage
- Damages every valid Enemy inside the radius

For the initial prototype, both Skills may apply damage instantly.

Do not implement projectile travel, falling-arrow simulation, final VFX, or particle effects yet.

## 5. Skill Aiming Flow

Required interaction:

```text
Select Skill Card
    -> LMB Down in valid gameplay area
        -> enter Aiming Mode
        -> show AoE indicator
        -> indicator follows cursor while LMB is held
    -> LMB Release
        -> valid gameplay area: Attempt Cast
        -> originating Skill Card: Cancel
        -> other UI: Cancel
```

Selecting a Skill Card alone must not cast it.

## 6. Aiming Indicator

While LMB is held in Aiming Mode:

- Show a circular range indicator.
- Follow the mouse cursor in world/gameplay space.
- Use the Skill's actual configured Radius for the indicator size.

Use one source of truth:

```text
SkillDefinition.Radius
    -> AoE gameplay radius
    -> Aiming indicator radius
```

Fireball should visibly have a smaller indicator than Arrow Rain.

## 7. Valid Cast Area

For the current prototype, a Skill can be cast anywhere inside the valid gameplay world area.

An Enemy does not need to be inside the AoE.

Casting into an empty valid area is still a successful cast:

- Elixir is spent.
- Card cycles.
- No Enemy is damaged if the area is empty.

Releasing over UI must cancel instead of casting underneath the UI.

## 8. Skill Cancellation

Primary cancellation flow:

1. Select Skill Card.
2. Hold LMB and enter Aiming Mode.
3. Move the cursor back over the originating Skill Card.
4. Release LMB.

Result:

```text
No Elixir spent
No Skill effect
No card rotation
Skill remains in Hand
Exit Aiming Mode
Hide indicator
```

Also cancel if LMB is released over other UI.

## 9. Insufficient Elixir

Before committing a valid cast, check affordability through the Elixir system.

If insufficient:

```text
No cast
No damage
No Elixir spent
No card cycle
Skill remains in Hand
Exit Aiming Mode
Hide indicator
Display "Not enough Elixir."
```

Message timing/fade is defined in `Elixir.md`.

## 10. Successful Cast

For a valid cast with enough Elixir:

```text
Validate position
    -> validate Elixir
    -> commit Skill use
    -> spend Elixir
    -> cycle Card
    -> execute Skill according to CastingTime
```

Skill Cards must use the same Hand-cycle success contract as Tower Cards.

## 11. Casting Time

Every Skill supports configurable `CastingTime`.

Current prototype:

```text
Fireball CastingTime = 0
Arrow Rain CastingTime = 0
```

Future-capable behavior:

```text
Successful cast commitment
    -> spend Elixir
    -> cycle Card
    -> wait CastingTime
    -> apply Skill effect
```

Example future Skill:

```text
Meteor
CastingTime = 1.5 seconds
```

Do not re-check Elixir after a cast has already been committed.

## 12. Damage Application

Both prototype Skills deal AoE damage to every valid Enemy inside the circular radius.

Reuse the Enemy system's common damage API.

Do not directly manipulate Enemy HP from UI code.

Conceptually:

```text
Skill Effect
    -> find valid enemies in radius
    -> call Enemy damage method/interface
```

Keep the design extensible for future:

- Slow
- Stun
- Damage over time
- Healing
- Buff/debuff
- Knockback

Do not implement those effects now.

## 13. Hand Integration

Skill Cards and Tower Cards share the same Hand.

Example:

```text
[Fireball, Test Tower 2, Arrow Rain]
```

Successful Skill cast:

```text
-> cycle used Skill Card
-> draw next Card
```

Cancelled/rejected Skill:

```text
-> no cycle
```

Do not create a separate Skill-only Hand.

## 14. OOP Requirements

Use OOP actively but avoid over-engineering.

Recommended principles:

- Common Skill abstraction
- Specialized Fireball/Arrow Rain effect behavior where necessary
- Shared casting pipeline
- Skill-specific effect logic separated from generic aiming/casting logic
- Reuse common Enemy damage APIs
- Skill Cards remain separate from Tower runtime inheritance

Do not copy the entire casting pipeline into each Skill subclass.

## 15. Scope Exclusions

Do not implement yet:

- Projectile flight
- Arrow projectile simulation
- Final VFX
- Skill animations
- Status effects
- Skill upgrades
- Skill rarity
- Multiple casts per card use
- Extra cooldown systems
- Touch drag-to-cast input

## 16. Verification

Verify at minimum:

1. Fireball and Arrow Rain exist as Skill Cards.
2. Both coexist with Tower Cards in PlayerDeck.
3. Fireball radius is smaller and damage higher.
4. Arrow Rain radius is larger and damage lower.
5. Selecting a Skill does not cast immediately.
6. LMB hold shows the aiming indicator.
7. Indicator follows the cursor.
8. Indicator radius matches gameplay radius.
9. Valid-area release attempts a cast.
10. Release over originating card cancels.
11. Release over other UI cancels.
12. Cancel spends no Elixir.
13. Cancel does not cycle the card.
14. Insufficient Elixir rejects cast.
15. Successful cast spends Elixir exactly once.
16. Successful cast cycles the card.
17. Both prototype Skills use CastingTime = 0.
18. AoE damages all valid Enemies in radius.
19. Empty-area cast is still successful.
20. Skill logic does not depend on Tower Placement Slots.

Report files changed, Skill hierarchy, aiming/casting state flow, Fireball/Arrow Rain values, Hand integration, tests performed, and unverified behavior.

## Translation note

The English messages above translate the originally specified Korean UI messages. This document translation does not request a change to the runtime UI language. Decode the Unicode escapes below to recover the exact original text.

- English meaning: `Not enough Elixir.` Original required UI text (Unicode-escaped): `\uc5d8\ub9ad\uc11c\uac00 \ubd80\uc871\ud569\ub2c8\ub2e4`.
