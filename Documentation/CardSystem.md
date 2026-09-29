# Card System Generalization

## Purpose

Generalize the existing tower-only deck/hand system into a reusable card system that supports both Tower Cards and Skill Cards.

Preserve the architecture and behavior defined in:

- `Main.md`
- `Tower.md`
- `enemy.md`
- `Lobby.md`
- `Deck.md`
- `HandCycle.md`

Do not rewrite working gameplay systems unless integration requires it.

## 1. Card Architecture

Introduce a common card abstraction:

```text
CardDefinition
├── TowerCardDefinition
└── SkillCardDefinition
```

Do not make runtime `Turret` objects and runtime `Skill` effects inherit directly from one another.

Use references instead:

```text
TowerCardDefinition
    -> TowerDefinition
        -> Turret runtime hierarchy

SkillCardDefinition
    -> SkillDefinition
        -> Skill runtime hierarchy
```

The Card layer represents objects that can exist in the Deck and Hand.

## 2. Common Card Data

`CardDefinition` should support common data such as:

- Stable card ID
- Display name
- Elixir Cost
- Card type
- Placeholder icon/color reference where useful

`ElixirCost` belongs at the common Card level.

Current prototype:

```text
Tower Cards:
ElixirCost = 0

Skill Cards:
ElixirCost = configurable non-zero value
```

Tower costs must be changeable later without redesigning the Hand or Elixir systems.

## 3. PlayerDeck Rules

Generalize `PlayerDeck` from six unique TowerDefinitions to six unique CardDefinitions.

Required invariants:

```text
Deck size = exactly 6
All cards are unique
TowerCard count >= 1
SkillCard count >= 0
```

Valid examples:

```text
6 Tower + 0 Skill
5 Tower + 1 Skill
3 Tower + 3 Skill
1 Tower + 5 Skill
```

Invalid:

```text
0 Tower + 6 Skill
```

Duplicates are forbidden for every card type.

## 4. Minimum One Tower Rule

If the player tries to replace the final Tower Card with a Skill Card:

- Reject the edit.
- Preserve the current deck.
- Display:

`덱에 최소 1장의 Tower 카드가 들어가야 합니다`

The message should remain visible for approximately 3 seconds and then fade out automatically.

Enforce this rule in deck/domain logic, not only in UI code.

## 5. Available Cards

Generalize the Deck Editor's available collection.

Conceptually:

```text
Available Cards
=
All unlocked/available CardDefinitions
-
CardDefinitions currently in PlayerDeck
```

A card already in the deck must not appear as another selectable copy.

After replacement:

- Incoming card disappears from Available Cards.
- Replaced card returns to Available Cards.
- Refresh immediately.

Do not maintain a separate manually synchronized available-card state.

## 6. Deck Editing Validation

Before committing drag/drop:

1. Resulting deck contains exactly 6 cards.
2. All cards are unique.
3. At least 1 Tower Card remains.

If validation fails:

- Revert the drag visually.
- Do not mutate `PlayerDeck`.
- Show the relevant feedback message.

## 7. Runtime Card Cycle

Generalize `RuntimeCardCycle` from TowerDefinitions to CardDefinitions.

Every stage run:

```text
PlayerDeck
    -> runtime copy
    -> shuffle once
    -> Hand[3]
    -> UpcomingQueue[3]
```

The runtime cycle contains exactly the same six unique cards as `PlayerDeck`.

Never modify `PlayerDeck` through runtime cycling.

## 8. Hand Rules

The Hand may contain any mix of Tower Cards and Skill Cards.

Example:

```text
[Fireball, Test Tower 2, Arrow Rain]
```

Only successful use advances the cycle.

Tower Card success:

```text
valid placement
-> spend ElixirCost
-> place turret
-> cycle
```

Skill Card success:

```text
valid cast
-> spend ElixirCost
-> commit/execute skill
-> cycle
```

Failure or cancellation:

```text
no Elixir spent
no cycle
```

## 9. Common Card Use Contract

Use a clean common result concept, for example:

```text
Success
Failure
Cancelled
```

Only `Success` may spend Elixir and advance the cycle.

Exact names may differ.

## 10. Tower Card Integration

Tower Cards continue using the existing placement and Turret architecture.

Do not move combat logic into Card UI.

Conceptually:

```text
TowerCardDefinition
    -> TowerDefinition

Hand UI selects card
    -> TowerPlacementController
    -> validate placement and Elixir
    -> place Turret
    -> report result
```

## 11. Skill Card Integration

Skill Cards use the architecture defined in `Skill.md`.

Conceptually:

```text
SkillCardDefinition
    -> SkillDefinition

Hand UI selects card
    -> Skill targeting/casting flow
    -> validate cast and Elixir
    -> commit Skill
    -> report result
```

Skill Cards do not use Tower Placement Slots.

## 12. OOP Requirements

Use:

- `CardDefinition` as the shared abstraction
- `TowerCardDefinition` and `SkillCardDefinition` as specializations
- Polymorphism where appropriate
- Encapsulated deck validation
- Composition/reference from cards to Tower/Skill definitions

Avoid deep inheritance and do not merge Tower runtime and Skill runtime classes.

## 13. Scope Exclusions

Do not implement yet:

- Card rarity
- Card leveling
- Duplicate copies
- Multiple deck presets
- Inventory quantities
- Crafting
- Upgrade trees
- Final card artwork

## 14. Verification

Verify at minimum:

1. PlayerDeck stores six CardDefinitions.
2. Tower and Skill Cards coexist.
3. All deck cards are unique.
4. Deck cannot contain zero Tower Cards.
5. Final-Tower replacement with Skill is rejected.
6. Warning fades after about 3 seconds.
7. Cards in deck are excluded from Available Cards.
8. Hand supports mixed card types.
9. Runtime shuffle includes all six unique cards once.
10. Tower success cycles.
11. Skill success cycles.
12. Failure/cancel does not cycle.
13. Runtime cycle does not modify PlayerDeck.

Report files changed, hierarchy/design, deck validation, Hand integration, tests performed, and unverified behavior.
