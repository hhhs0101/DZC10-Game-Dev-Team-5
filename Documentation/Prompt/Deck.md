# Deck Building System

## Purpose

Add a six-tower deck-building system to the Unity prototype.

Preserve the architecture defined in `Main.md`, `Tower.md`, `enemy.md`, and `Lobby.md`.

Use page 2 of `game ui.pdf` as the structural reference for the Deck Editor.

Treat the PDF as a layout reference only. Use placeholder UI and focus on data ownership, drag-and-drop behavior, and extensibility.

## 1. Deck Definition

The player has exactly six persistent deck slots:

```text
T1
T2
T3
T4
T5
T6
```

`T1` is the first/leftmost slot and `T6` the last/rightmost slot.

The six slots represent persistent deck organization only.

Gameplay draw order is defined separately in `HandCycle.md`.

## 2. Deck Editor Layout

Use a book-like two-page layout:

```text
┌──────────────────────────┬─────────────────────────┐
│                          │                         │
│    Available Towers      │      Current Deck       │
│                          │                         │
│    tower cards           │    T1        T2         │
│                          │    T3        T4         │
│                          │    T5        T6         │
│                          │                         │
└──────────────────────────┴─────────────────────────┘
```

Place:

- `Back to Lobby` in the top-left
- `Settings` in the top-right

Do not implement final artwork yet.

## 3. Available Towers

The left side displays tower types available to the player.

For the prototype, use existing test tower definitions.

Tower cards must reference existing tower configuration/data.

Do not duplicate combat logic in card UI.

The menu must support more tower definitions later without rewriting the whole editor.

## 4. Current Deck

The right side displays six clearly labeled slots:

```text
T1  T2
T3  T4
T5  T6
```

Each slot shows:

- Slot number
- Assigned tower
- Placeholder tower card representation

Slot positions must remain stable.

## 5. Drag-and-Drop Replacement

Allow an Available Tower card to be dragged onto a deck slot.

Example:

```text
Available:
Tower X

Deck:
T3 = Tower Y
```

Dragging `Tower X` onto `T3` produces:

```text
Deck:
T3 = Tower X

Available:
Tower Y
```

The replaced tower returns to Available Towers.

The new tower occupies exactly the replaced slot.

## 6. Swap Rules

1. Only the targeted slot changes
2. Replaced tower returns to Available Towers
3. New tower takes the exact slot position
4. Deck remains exactly six towers
5. Do not create accidental duplicate runtime card objects
6. Do not modify tower combat implementations when moving cards
7. Invalid/cancelled drops must leave deck data unchanged

If a drag is cancelled or dropped outside a valid target:

- Return the card to its original position
- Do not modify `PlayerDeck`

## 7. Deck State Must Not Live in UI

Create a dedicated deck model.

Conceptually:

```text
PlayerDeck
├── T1 -> TowerDefinition
├── T2 -> TowerDefinition
├── T3 -> TowerDefinition
├── T4 -> TowerDefinition
├── T5 -> TowerDefinition
└── T6 -> TowerDefinition
```

The same `PlayerDeck` must be read by:

- Lobby deck preview
- Deck Editor
- Gameplay hand initialization

UI represents deck state; it does not own it.

## 8. Tower Definitions

Deck slots reference tower definitions/configuration, not combat instances.

Conceptually:

```text
TowerDefinition
├── Name
├── Cost
├── Damage
├── Range
├── Attack Rate
└── Other static properties
```

Runtime turret GameObjects are still created through the existing placement system.

## 9. Navigation

### Back to Lobby

Pressing `Back to Lobby` should:

- Preserve current deck configuration
- Return to Lobby
- Refresh Lobby deck preview if required

### Settings

Use the existing Settings architecture.

Returning from Settings must return to Deck Editor without losing deck state.

Deck changes may apply immediately; no separate Save button is required for now.

## 10. Session Persistence

The deck must persist during the current application session when the player:

- Opens/closes Settings
- Returns to Lobby
- Enters a stage
- Exits a stage
- Re-enters Deck Editor

Permanent disk persistence is not required yet.

## 11. Architecture Rules

Keep responsibilities separated:

```text
TowerDefinition
    -> static tower data

PlayerDeck
    -> six persistent deck assignments

DeckEditorController
    -> deck editing operations

DeckEditorUI
    -> visual drag/drop representation

LobbyDeckPreview
    -> read-only PlayerDeck presentation

GameplayHand
    -> battle runtime state derived from PlayerDeck
```

Deck Editor must not own:

- Turret combat
- Tower placement validation
- Enemy logic
- Stage progression
- Gameplay resources

## 12. OOP and Extensibility

Use encapsulation for deck state.

Use events/interfaces where useful to keep UI synchronized.

Avoid unnecessary UI inheritance hierarchies.

The architecture should later support:

- More towers
- Tower unlocks
- Multiple saved decks
- Deck presets
- Tower rarity
- Tower upgrades

Do not implement those features yet.

## 13. Scope Exclusions

Do not implement yet:

- Multiple deck presets
- Permanent save/load
- Tower rarity
- Tower leveling
- Upgrade trees
- Inventory quantities
- Crafting
- Final card art
- Final book UI art

## 14. Verification

Verify at minimum:

1. Deck Editor opens from Lobby
2. Available Towers appear on left
3. T1-T6 appear on right
4. Each slot displays assigned tower
5. Dragging an Available Tower onto a slot replaces it
6. Replaced tower returns to Available Towers
7. Invalid/cancelled drag does not alter deck
8. Deck always contains six valid entries
9. Returning to Lobby preserves deck
10. Lobby preview reflects changes
11. Gameplay reads same PlayerDeck
12. Deck UI does not instantiate combat turrets directly

Report:

- Files created or modified
- New data structures
- Drag-and-drop flow
- Deck state ownership
- UI synchronization
- What was actually tested
- Anything not verified
