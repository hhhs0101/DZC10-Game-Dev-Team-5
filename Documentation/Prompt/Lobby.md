# Lobby and Stage Carousel Expansion

## Purpose

Extend the existing Unity prototype by introducing a dedicated Lobby layer between the Start Screen and gameplay.

Preserve the existing architecture and the design principles defined in `Main.md`, `Tower.md`, and `enemy.md`.

Use `game ui.pdf` as the structural UI reference. Treat it as a layout reference only; use placeholder UI and do not treat it as final art direction.

Do not rewrite working gameplay systems unless integration requires it.

## 1. Updated Game Flow

```text
Start Screen
    -> Start Game
        -> Lobby
            -> Stage Carousel
            -> Current Deck Preview
            -> Deck Editor
            -> Settings
            -> Enter Selected Stage
                -> Gameplay
                    -> 3-Card Hand
                    -> Tower Placement
                    -> Pause
```

Pressing `Start Game` should open the Lobby instead of entering gameplay directly.

## 2. Lobby Layout

Use page 1 of `game ui.pdf` as the structural reference.

The Lobby contains:

- Stage Carousel in the upper/central area
- Current Deck Preview below the carousel
- Deck button associated with the deck area
- Settings button in the top-right corner

Preserve Lobby state when opening Deck Editor or Settings and returning.

## 3. Stage Carousel

Display three stages at a time:

```text
Previous Stage | Selected Stage | Next Stage
```

The selected stage always occupies the center position.

Visually emphasize the center stage, for example with larger scale, stronger opacity, border, or slight vertical offset.

Use the internal term `Selected Stage`, not `Current Stage`.

Recommended state variable:

```text
selectedStageIndex
```

## 4. Stage Navigation

### A Key

Pressing `A` moves one stage backward.

```text
Before:
1-1 | 1-2 | 1-3
      center

After A:
     | 1-1 | 1-2
       center
```

### D Key

Pressing `D` moves one stage forward.

```text
Before:
1-1 | 1-2 | 1-3
      center

After D:
1-2 | 1-3 | 1-4
      center
```

## 5. Held-Key Navigation

Holding `A` or `D` should repeatedly navigate.

Do not move every frame.

Use:

1. Immediate movement on key press
2. Initial hold delay
3. Fixed repeat interval

Example configurable values:

```text
Initial hold delay: 0.4 seconds
Repeat interval: 0.15 seconds
```

## 6. Carousel Animation

Do not teleport stage cards between positions.

Animate smoothly between left, center, and right positions.

If center emphasis uses scale, animate scale together with position.

The animation must support arbitrary stage counts without individually hard-coded stage animations.

Keep stage data separate from presentation/animation.

## 7. Stage Entry

Pressing `Enter` attempts to enter the centered stage.

If unlocked:

- Load the stage

If locked:

- Do not load it
- Show simple locked-stage feedback

## 8. Stage Progression

Introduce a minimal progression model.

For now:

- The first stage is unlocked
- Completing a stage unlocks the required next stage
- Locked stages remain visible but cannot be entered

Example:

```text
Complete 1-1 -> Unlock 1-2
Complete 1-2 -> Unlock 1-3
```

Support future stage sequences such as:

```text
1-1
1-2
1-3
...
2-1
2-2
...
```

Keep progression state separate from carousel UI.

## 9. Current Deck Preview

Below the carousel, display six persistent deck slots:

```text
T1 | T2 | T3 | T4 | T5 | T6
```

`T1` is leftmost and `T6` is rightmost.

Each slot displays its assigned tower.

These are persistent deck positions only. They do not directly define gameplay draw order.

The preview must read from the same `PlayerDeck` state used by the Deck Editor.

## 10. Deck Button

Provide a `Deck` button in or near the deck preview.

Pressing it opens the Deck Editor.

When returning:

- Preserve selected stage
- Preserve deck configuration
- Preserve carousel position

Detailed behavior is defined in `Deck.md`.

## 11. Settings

Place a Settings button in the top-right corner.

Reuse the existing Settings architecture where practical.

Returning from Settings must not reset the Lobby state.

## 12. Architecture

Recommended responsibility separation:

```text
GameFlow
├── Start Screen
├── Lobby
├── Deck Editor
├── Settings
└── Gameplay

Lobby
├── Stage Carousel
├── Stage Progression
└── Deck Preview
```

Do not create a God Object for Lobby behavior.

Separate:

- Lobby navigation
- Stage selection/progression
- Carousel animation/presentation
- Deck preview presentation

## 13. Data vs Runtime State

Use ScriptableObjects where appropriate for static definitions such as Stage and Tower definitions.

Do not use UI objects as authoritative state.

Conceptually:

```text
StageDefinition -> static metadata
StageProgressionState -> runtime unlock/completion
PlayerDeck -> session deck configuration
Lobby UI -> representation only
```

## 14. Scope Exclusions

Do not implement yet:

- Final Lobby artwork
- Final card artwork
- Touch/swipe controls
- Permanent save files
- Multiple deck presets
- World map navigation
- Matchmaking
- Online functionality
- Advanced progression rewards

Keyboard input is sufficient for the current prototype.

## 15. Verification

Verify at minimum:

1. Start Game opens Lobby
2. Lobby shows Stage Carousel
3. A moves backward
4. D moves forward
5. Holding A/D repeats correctly
6. Stage cards animate instead of teleporting
7. Selected stage stays centered
8. Enter loads unlocked centered stage
9. Locked stages cannot be entered
10. Lobby displays T1-T6
11. Deck opens Deck Editor
12. Returning preserves Lobby state
13. Settings opens and returns correctly

Report:

- Files created or modified
- Scenes/prefabs/ScriptableObjects added
- Carousel architecture
- Progression architecture
- How Lobby reads deck state
- What was actually tested
- Anything not verified
