# Lobby, Deck, Hand Cycle, and Retry Patch

## Purpose

Apply the following fixes and interaction improvements to the existing Unity project.

Preserve the architecture and behavior defined in:

- `Main.md`
- `Tower.md`
- `enemy.md`
- `Lobby.md`
- `Deck.md`
- `HandCycle.md`

Modify the existing systems rather than replacing them.

---

## 1. Prevent Duplicate Tower Cards in the Gameplay Hand

### Problem

The same tower type can appear more than once in the current Hand.

Invalid example:

```text
Hand:
[Tower 1, Tower 2, Tower 1]
```

### Required Fix

Enforce this invariant:

```text
Every card in the current Hand must reference a unique TowerDefinition.
```

At stage start:

1. Read the six unique entries from `PlayerDeck`.
2. Create a runtime copy.
3. Shuffle that copy once.
4. Verify that the runtime cycle contains no duplicate `TowerDefinition`.
5. Use the first three entries as the Hand.
6. Use the remaining three entries as the Upcoming Queue.

During rotation:

- Do not insert a tower into the Hand if the same `TowerDefinition` is already present.
- Preserve uniqueness across the full runtime cycle.
- Do not create duplicate logical card entries for the same tower definition.

Add defensive validation/assertions where useful so duplicate states are detected early.

---

## 2. Prevent Duplicate Towers in PlayerDeck

The same tower type must not occupy multiple deck slots.

Invalid:

```text
T1 = Tower A
T2 = Tower B
T3 = Tower A
```

Valid:

```text
T1 = Tower A
T2 = Tower B
T3 = Tower C
```

Enforce:

```text
PlayerDeck contains exactly six unique TowerDefinition references.
```

When editing the deck:

- Reject or correctly handle any operation that would create a duplicate.
- Do not leave the deck in an invalid intermediate state.
- After every successful edit, the deck must still contain six unique towers.

---

## 3. Available Towers Must Exclude Towers Already in Current Deck

The `Available Towers` list should contain only towers not currently assigned to any of the six deck slots.

Conceptually:

```text
Available Towers
=
All unlocked/available TowerDefinitions
-
TowerDefinitions currently in PlayerDeck
```

Example:

```text
All Towers:
A B C D E F G H

Current Deck:
A B C D E F

Available Towers:
G H
```

If Tower G replaces Tower C in T3:

```text
Before:
T3 = Tower C
Available = [G, H]

After:
T3 = Tower G
Available = [C, H]
```

The UI must refresh immediately after a successful deck edit.

Do not manually maintain a separate Available Towers list that can drift out of sync. Derive it from authoritative tower data and `PlayerDeck`.

---

## 4. Mouse Navigation for Stage Carousel

Keep existing keyboard behavior:

- `A` -> previous stage
- `D` -> next stage

Add mouse navigation.

### Clicking Previous Stage

Clicking the visible stage card on the left should:

- Perform the same logical action as one `A` step.
- Animate normally.
- Move that stage into the center.

### Clicking Next Stage

Clicking the visible stage card on the right should:

- Perform the same logical action as one `D` step.
- Animate normally.
- Move that stage into the center.

Keyboard and mouse must share the same underlying navigation methods.

Conceptually:

```text
A key --------------------\
                           -> MovePrevious()
Click previous stage -----/

D key --------------------\
                           -> MoveNext()
Click next stage ---------/
```

Do not duplicate carousel state logic for mouse input.

Clicking the centered stage does not need to move the carousel.

---

## 5. Retry Option After Defeat

Add a `Retry` option to the Game Over/Defeat UI.

Retry should start a fresh run of the same stage.

Reset all stage-runtime state, including at minimum:

- Base HP
- Gameplay resources
- Spawn state
- Existing enemies
- Existing turrets
- Tower placement slot occupancy
- Pause/Game Over state
- Runtime Hand
- Upcoming Queue
- Other temporary stage-specific state

Do not modify:

- `PlayerDeck`
- Deck slot assignments
- Session stage progression due to defeat

Because Retry is a new stage run, rebuild the gameplay hand according to `HandCycle.md`:

```text
PlayerDeck
    -> new runtime copy
        -> shuffle once
            -> new Hand
            -> new Upcoming Queue
```

Retry may therefore produce a different initial Hand from the failed run.

The same stage remains selected.

---

## 6. Required State Relationships

Preserve the following source-of-truth structure:

```text
TowerDefinitions
    -> all tower definitions

PlayerDeck
    -> exactly six unique TowerDefinitions

Available Towers UI
    -> TowerDefinitions minus PlayerDeck

RuntimeCardCycle
    -> runtime copy of PlayerDeck
    -> shuffled once per stage run
    -> same six unique tower definitions

Hand
    -> three unique entries from RuntimeCardCycle

Upcoming Queue
    -> remaining runtime entries

Stage Carousel
    -> one shared navigation state
    -> keyboard and mouse use same navigation logic
```

UI must not become the authoritative storage for deck or hand state.

---

## 7. Do Not Change

Do not change these systems unless required by the fixes above:

- Turret combat architecture
- Enemy architecture
- Resource-cost rules
- Tower placement validation
- Lobby structure
- Stage unlock rules
- Existing A/D held-key navigation
- Existing carousel animation style
- Existing Settings architecture

Do not add unrelated gameplay features or final visual polish.

---

## 8. Verification

### Deck Uniqueness

1. The same `TowerDefinition` cannot occupy two PlayerDeck slots.
2. Drag/drop cannot create duplicate deck towers.
3. PlayerDeck remains six unique tower definitions.

### Available Towers

4. Towers in PlayerDeck do not appear in Available Towers.
5. A newly added deck tower disappears from Available Towers immediately.
6. A replaced deck tower appears in Available Towers immediately.
7. Repeated replacements remain synchronized.

### Hand Cycle

8. Runtime cycle starts with six unique tower definitions.
9. Hand always contains three unique tower definitions.
10. Rotation cannot introduce a duplicate into Hand.
11. Successful placement still advances the cycle.
12. Failed placement still leaves Hand and Upcoming Queue unchanged.
13. PlayerDeck is not modified by gameplay rotation.

### Stage Carousel

14. A still moves backward.
15. D still moves forward.
16. Clicking the left stage performs the same action as A.
17. Clicking the right stage performs the same action as D.
18. Mouse navigation uses the existing animation.
19. Keyboard and mouse do not create conflicting carousel state.

### Retry

20. Retry appears after defeat.
21. Retry restarts the same stage.
22. Base HP and resources reset.
23. Old enemies and turrets are removed.
24. Placement slots reset.
25. Runtime Hand/Queue are recreated.
26. A new one-time shuffle occurs.
27. PlayerDeck remains unchanged.

---

## 9. Implementation Report

After implementation, report:

1. Files created or modified.
2. Root cause of the duplicate Hand bug.
3. How deck uniqueness is enforced.
4. How Available Towers is derived from PlayerDeck.
5. How mouse carousel input reuses existing navigation logic.
6. How Retry resets stage-runtime state.
7. Tests actually performed.
8. Any behavior that could not be verified.
