# Gameplay Hand and Randomized Card Cycle

## Purpose

Add a three-card gameplay hand derived from the player's six-tower deck.

Preserve the systems defined in `Main.md`, `Tower.md`, `enemy.md`, `Lobby.md`, and `Deck.md`.

The persistent six-slot deck must remain unchanged during gameplay.

At the start of each stage run, copy the deck, shuffle the runtime copy once, and use that shuffled order to initialize a cyclic three-card hand.

Do not reshuffle during the stage.

## 1. Persistent Deck vs Runtime Battle Order

Persistent Lobby deck:

```text
PlayerDeck:
T1 T2 T3 T4 T5 T6
```

Do not modify this order when gameplay starts.

At stage start:

```text
PlayerDeck
    -> Runtime Copy
        -> Shuffle Once
            -> RuntimeCardCycle
```

Example:

```text
PlayerDeck:
T1 T2 T3 T4 T5 T6

Possible runtime shuffle:
T3 T2 T5 T1 T4 T6
```

All six entries must appear exactly once.

Use an unbiased C#/Unity shuffle such as Fisher-Yates.

## 2. Initial Hand

The first three shuffled entries become the Hand.

The remaining three become the Upcoming Queue.

```text
Shuffled Runtime Order:
T3 T2 T5 T1 T4 T6

Hand:
[T3, T2, T5]

Upcoming Queue:
[T1, T4, T6]
```

Only the three Hand cards are visible/selectable in gameplay.

## 3. Cyclic Card Rule

After the initial shuffle, the cycle is deterministic.

Do not reshuffle again during the stage.

When a card is successfully used:

1. Remove the selected card from Hand
2. Draw the first Upcoming card
3. Add the drawn card to the right side of Hand
4. Move the used card to the back of Upcoming

Example:

```text
Before:

Hand:
[T3, T2, T5]

Upcoming:
[T1, T4, T6]
```

Use `T2` successfully:

```text
After:

Hand:
[T3, T5, T1]

Upcoming:
[T4, T6, T2]
```

Then use `T5`:

```text
Hand:
[T3, T1, T4]

Upcoming:
[T6, T2, T5]
```

Continue this cycle throughout the stage.

## 4. Successful Use Condition

A card cycles only after successful tower placement.

Successful placement means:

- Location is valid
- Slot is not occupied
- Player has enough resources
- Turret was actually placed successfully

Only then advance the card cycle.

## 5. Failed Placement

If placement fails due to:

- Invalid location
- Occupied slot
- Insufficient resources
- Cancellation
- Other validation failure

then:

- Keep the card in Hand
- Do not draw a new card
- Do not modify Upcoming Queue
- Do not change cycle order

Example:

```text
Hand:
[T3, T2, T5]

Upcoming:
[T1, T4, T6]

T2 placement fails

Result:

Hand:
[T3, T2, T5]

Upcoming:
[T1, T4, T6]
```

## 6. New Stage Run

Every new stage run creates a new shuffled runtime order.

Example:

```text
Run 1:
T3 T2 T5 T1 T4 T6

Run 2:
T6 T1 T3 T4 T2 T5
```

Persistent Lobby deck remains:

```text
T1 T2 T3 T4 T5 T6
```

Restarting or re-entering a stage should generate a new runtime shuffle unless a future deterministic-seed feature says otherwise.

## 7. Gameplay Hand UI

Replace the current always-visible turret list with a three-card Hand UI.

Each card should show enough placeholder information to identify it:

- Tower name
- Cost
- Placeholder icon/color

The player selects a Hand card, then uses the existing tower placement flow.

Do not add final card art yet.

## 8. Integration with Tower Placement

Do not put placement/combat logic inside Hand UI.

Use:

```text
Hand UI
    -> selects TowerDefinition

TowerPlacementController
    -> validates placement

Successful placement
    -> notifies RuntimeCardCycle

RuntimeCardCycle
    -> updates Hand and Upcoming Queue

Turret runtime object
    -> handles combat
```

The Hand works with tower definitions, not pre-instantiated turret GameObjects.

## 9. Runtime Data Model

Create a dedicated runtime battle-cycle model:

```text
RuntimeCardCycle
├── Hand[3]
└── UpcomingQueue[3]
```

The runtime model owns cycle state.

UI only renders/observes it.

## 10. Randomness and Debugging

Normal gameplay uses a new randomized order for each run.

Keep the architecture compatible with an optional fixed random seed for tests/debugging later.

Do not expose seed controls in normal gameplay yet.

## 11. Meaning of T1-T6

Important distinction:

```text
T1-T6
    = persistent Deck Editor slot positions

Runtime shuffled order
    = battle draw order for one stage run
```

Do not interpret `T1` as always being the first card drawn.

Example:

```text
PlayerDeck:
T1 T2 T3 T4 T5 T6

Battle shuffle:
T4 T1 T6 T2 T5 T3
```

## 12. Architecture Rules

Keep responsibilities separated:

```text
PlayerDeck
    -> persistent six-slot composition

RuntimeCardCycle
    -> shuffled battle-specific order

Hand UI
    -> display and selection

TowerPlacementController
    -> placement validation

Resource System
    -> cost validation/deduction

Turret
    -> combat behavior
```

Do not modify `PlayerDeck` when runtime cards cycle.

Do not let Hand UI bypass placement/resource validation.

## 13. Scope Exclusions

Do not implement yet:

- More than three Hand cards
- Card rarity
- Manual reroll
- Manual discard
- Advanced card animations
- Special draw modifiers
- Card lockouts
- Multiple deck presets
- Mid-stage reshuffling

## 14. Verification

Verify at minimum:

1. Stage entry copies PlayerDeck rather than modifying it
2. Runtime copy is shuffled once
3. All six entries occur exactly once in initial runtime order
4. First three become Hand
5. Remaining three become Upcoming Queue
6. Successful placement cycles used card to back
7. Next queued card enters Hand
8. Failed placement does not change Hand
9. Failed placement does not change Upcoming Queue
10. No additional shuffle occurs during stage
11. Re-entering/restarting produces a new shuffled order
12. Returning to Lobby leaves T1-T6 unchanged
13. Hand UI contains no combat logic

Report:

- Files created or modified
- Runtime cycle data structure
- Shuffle implementation
- Hand/queue update flow
- Placement success/failure integration
- What was actually tested
- Anything not verified
