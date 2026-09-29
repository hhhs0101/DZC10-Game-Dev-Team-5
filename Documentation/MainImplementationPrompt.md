# Main Implementation Prompt — Card, Elixir, and Skill Systems

Read the existing project specifications first:

- `Main.md`
- `Tower.md`
- `enemy.md`
- `Lobby.md`
- `Deck.md`
- `HandCycle.md`
- `Patch_DeckLobbyHandRetry.md`

Then read the new specifications:

- `CardSystem.md`
- `Elixir.md`
- `Skill.md`

Treat all of these files as project specifications.

## Implementation Order

Before editing code, inspect the current Unity project and identify the existing classes/data responsible for:

- TowerDefinition / turret configuration
- PlayerDeck
- Available Cards/Towers
- RuntimeCardCycle
- Hand UI
- TowerPlacementController
- Existing resource system
- Enemy damage handling
- Game state / Pause / Game Over / Retry

Then implement in this order:

1. Generalize the Tower-only Card/Deck/Hand model into the Card architecture from `CardSystem.md`.
2. Add the shared Elixir system from `Elixir.md`.
3. Add the Skill runtime and Skill Card behavior from `Skill.md`.
4. Integrate Fireball and Arrow Rain into the Deck Editor and gameplay Hand.
5. Update focused tests and verification.

Extend the current architecture instead of replacing working systems.

## Required Architecture

The final conceptual structure should follow:

```text
CardDefinition
├── TowerCardDefinition
│      -> TowerDefinition
│          -> Turret runtime hierarchy
│
└── SkillCardDefinition
       -> SkillDefinition
           -> Skill runtime hierarchy
```

Do not make runtime Turret and Skill objects inherit from each other.

The common Card layer exists for Deck/Hand behavior.

## Required Deck Invariants

After implementation:

```text
PlayerDeck
- exactly 6 CardDefinitions
- all cards unique
- at least 1 TowerCard
- 0 to 5 SkillCards
```

If the player attempts to replace the final Tower Card with a Skill Card:

- reject the edit,
- preserve the previous deck,
- display `덱에 최소 1장의 Tower 카드가 들어가야 합니다`,
- fade the message out after approximately 3 seconds.

Cards already in the deck must not appear in the Available Cards collection.

## Runtime Hand

Every fresh stage run:

```text
PlayerDeck
    -> runtime copy
    -> shuffle once
    -> Hand[3] + UpcomingQueue[3]
```

The six runtime entries remain unique.

Do not reshuffle during the stage.

Tower Cards and Skill Cards use the same Hand and cyclic queue.

Only successful Card use advances the cycle.

## Elixir

Use:

```text
Minimum = 0
Starting = 3
Maximum = 10
Regeneration = 0.36 / second
```

Elixir regenerates only during active gameplay.

Pause and Game Over stop regeneration.

Tower Card Elixir costs must be supported through the common Card cost field, but set existing Tower Cards to `0` for now.

Skill Cards use configurable non-zero Elixir costs.

If the player cannot afford a Card:

- reject the action,
- spend no Elixir,
- do not cycle the Card,
- display `엘릭서가 부족합니다`,
- fade the message out after approximately 1.5 seconds.

Spend Elixir only after Card use has been successfully committed.

Retry resets Elixir to 3.

## Skill Input Flow

Skill use follows:

```text
Select Skill Card
    -> LMB Down in gameplay area
    -> enter Aiming Mode
    -> show AoE indicator
    -> indicator follows cursor while held
    -> LMB Release
```

Release behavior:

```text
Valid gameplay area:
    -> attempt cast

Originating Skill Card:
    -> cancel

Other UI:
    -> cancel
```

Cancellation means:

```text
No Elixir spent
No card cycle
No Skill effect
Skill remains in Hand
```

## Prototype Skills

### Fireball

- Circular AoE
- Smaller radius
- Higher instant damage
- CastingTime = 0

### Arrow Rain

- Circular AoE
- Larger radius
- Lower instant damage
- CastingTime = 0

Both affect every valid Enemy inside their radius.

Both should be configurable.

## Casting Time Contract

Support future non-zero CastingTime.

Use:

```text
Commit successful cast
    -> spend Elixir
    -> cycle Card
    -> wait CastingTime
    -> apply Skill effect
```

For Fireball and Arrow Rain, CastingTime is currently zero.

Do not re-check Elixir after a cast has already been committed.

## Elixir Bar

Add a vertical Elixir Bar in the open right-side gameplay UI area.

The fill should rise smoothly toward the actual Elixir level.

Keep authoritative Elixir state separate from the animated UI fill.

A simple liquid-like vertical fill is sufficient.

Do not build an advanced liquid shader or final VFX.

## Preserve Existing Systems

Do not unnecessarily rewrite:

- Turret inheritance/combat
- Enemy inheritance/damage
- Tower placement slots
- Stage Carousel
- Lobby
- Pause
- Retry
- Stage progression

Only update integrations required for the generalized Card and Elixir/Skill systems.

## Testing

Run existing relevant tests and add focused tests where practical.

Verify at minimum:

- mixed Tower/Skill decks,
- no duplicate Cards,
- minimum one Tower rule,
- Available Cards synchronization,
- runtime shuffle uniqueness,
- mixed Hand cycling,
- Elixir start/min/max/regeneration,
- Pause/Game Over regeneration stop,
- insufficient-Elixir rejection,
- successful Tower Card usage,
- Fireball aiming/cast/cancel,
- Arrow Rain aiming/cast/cancel,
- Skill AoE damage,
- Skill card cycling,
- Retry Elixir reset and new runtime shuffle.

Do not claim manual mouse/UI behavior was tested unless it was actually tested.

## Final Report

After implementation, report:

1. Existing classes that were reused.
2. Files created or modified.
3. Final Card class/data hierarchy.
4. PlayerDeck validation design.
5. Elixir architecture.
6. Skill architecture.
7. Fireball/Arrow Rain test values.
8. Hand-cycle integration.
9. Automated tests performed.
10. Manual behavior actually verified.
11. Anything that remains unverified.
