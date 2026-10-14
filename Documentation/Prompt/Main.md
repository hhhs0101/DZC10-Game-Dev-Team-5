# Unity 2D Defense Game - Main Specification

## 1. Project Goal

Build the basic structure of a 2D defense game in Unity.

The current goal is **not** to create a complete game. The goal is to create a clean, modular, object-oriented foundation that can be extended later as new gameplay ideas are added.

Visual assets will be added later.

For the current prototype:

- Use simple Unity primitives, sprites, colors, and placeholder UI.
- Do not spend time on visual polish.
- Do not implement animation, particle effects, sound effects, or final UI assets unless explicitly requested later.
- Prioritize functional gameplay, clean responsibilities, extensibility, and simplicity.
- Do not over-engineer the prototype.

For detailed turret design, refer to `Tower.md`.

For detailed enemy design, refer to `enemy.md`.

---

## 2. Core Gameplay Loop

The basic gameplay loop is:

1. Enemies spawn from a spawn point.
2. Enemies move automatically along a predefined path toward the player's Base.
3. The player selects a turret from the build/create UI.
4. The player places the turret on a predefined Tower Placement Slot.
5. Turrets automatically detect and attack valid enemies within range.
6. Enemies whose HP reaches 0 are removed.
7. Enemies that reach the end of the path damage the player's Base and are removed.
8. If Base HP reaches 0, the game enters the Game Over state.

A full victory condition is not required yet.

---

## 3. Base and Game Over

The player has a Base with a simple HP value.

When an enemy reaches the end of its path:

- It deals its Base Damage to the Base.
- It is removed from the stage.

When Base HP reaches 0:

- Enter the Game Over state.
- Stop enemy spawning.
- Stop normal gameplay interactions.
- Display a simple Game Over UI.

The architecture should allow future addition of:

- Victory
- Retry
- Stage Results
- Rewards
- Progression

without requiring a major rewrite.

---

## 4. Resource System

Implement a minimal resource system.

The player starts each test stage with a fixed amount of resource.

Turrets have a Cost value.

When placing a turret:

- If the player has enough resource, subtract the cost and place the turret.
- If the player does not have enough resource, do not place the turret and display:

`Not enough resources.`

Do not implement a complex economy system yet.

Do not implement enemy kill rewards or passive income unless explicitly requested later.

Keep the resource system extensible so these features can be added later.

---

## 5. Enemy Spawning

Do not implement a complex wave system yet.

For the current prototype:

- Spawn enemies periodically.
- Use a fixed spawn interval.
- Spawn enough enemies to test movement, combat, placement, Base damage, and Game Over.

Separate **spawn execution logic** from **stage/wave configuration**.

The intended future structure is:

```text
Stage Definition
    -> Wave Definitions
        -> Enemy Spawn Instructions
            -> EnemySpawner
```

In the future, stage and wave information may come from:

- JSON
- ScriptableObjects
- Other data-driven formats

Do **not** implement JSON-based stage loading yet.

The current implementation should ensure that a future data-driven wave system can be added without rewriting the core `EnemySpawner`.

---

## 6. Main Menu

When the game starts, show a simple Main Menu.

The Main Menu contains:

- Start Game
- Settings
- Exit

Use placeholder UI only.

### 6.1 Start Game

When the player presses `Start Game`, open a Stage Selection screen.

The intended future stage list may look like:

```text
1-1
1-2
1-3
...
2-1
2-2
...
```

For the current prototype, only show:

`Test Stage`

Selecting `Test Stage` starts the current playable prototype.

The Stage Selection system must be extensible.

Do not hard-code the overall UI architecture around a single Test Stage.

Future stage information may eventually be loaded from stage definitions, ScriptableObjects, JSON, or another data source.

Do not implement the full stage data system yet.

### 6.2 Settings

The Settings button should open a simple placeholder Settings panel or screen.

Future Settings may include:

- Resolution
- Fullscreen / Windowed mode
- Master Volume
- Music Volume
- Sound Effect Volume

Do not implement the actual resolution or sound settings until explicitly requested.

For now, only create the navigation structure required to enter and leave Settings.

### 6.3 Exit

The Exit button exits the application.

Handle Unity Editor execution appropriately because `Application.Quit()` does not terminate the Editor play session by itself.

---

## 7. In-Game Pause System

During gameplay, display a Pause button in the top-right corner.

The player can enter Pause using either:

- The on-screen Pause button
- The Escape key

Both inputs must call the same pause logic.

Do not create separate pause implementations.

When paused:

- Pause normal gameplay.
- Stop enemy movement.
- Stop enemy spawning.
- Stop turret attacks.
- Stop other time-dependent gameplay.
- Display a semi-transparent dark overlay.
- Display a centered Pause Menu.

The Pause Menu contains:

- Resume
- Settings
- Exit

### 7.1 Resume

Resume should:

- Close the Pause Menu.
- Remove the dark overlay.
- Continue gameplay from the same state.

Pressing Escape while already paused should also resume the game.

### 7.2 Settings from Pause

The Settings button should reuse the same Settings system used by the Main Menu where practical.

Returning from Settings must return to the Pause Menu.

It must **not** automatically resume gameplay.

### 7.3 Exit from Pause

Pressing Exit from the Pause Menu must not immediately leave the stage.

Show a confirmation dialog:

`Really exit this stage?`

Buttons:

- Yes
- No

If `Yes`:

- Leave the stage.
- Return to the Main Menu.

If `No`:

- Close the confirmation dialog.
- Return to the Pause Menu.
- Keep the game paused.

---

## 8. Game State / Flow Structure

Keep gameplay states clearly separated.

At minimum, support the logical states:

- Main Menu
- Stage Selection
- Playing
- Paused
- Game Over

Avoid scattering game-state checks across unrelated scripts.

Use a central game-flow or game-state responsibility where appropriate, but do not create an unnecessarily complex state-management framework.

The architecture should allow future states such as:

- Victory
- Stage Results
- Loading
- Retry
- Stage Unlock
- Save / Load progression

The intended flow is:

```text
Main Menu
├── Start Game
│   └── Stage Selection
│       └── Test Stage
│           ├── Playing
│           ├── Pause
│           │   ├── Resume
│           │   ├── Settings
│           │   └── Exit Confirmation
│           │       ├── Yes -> Main Menu
│           │       └── No -> Pause Menu
│           └── Game Over
├── Settings
└── Exit
```

---

## 9. Scene and UI Organization

Before implementation, propose an appropriate Unity scene structure.

For example:

- Main Menu scene
- Gameplay scene

or another simple scene structure if there is a better reason.

Explain the chosen structure before implementing it.

Keep:

- Menu UI logic
- Gameplay logic
- Stage selection logic
- Settings navigation
- Pause logic

reasonably separated.

---

## 10. Object-Oriented Design Principles

Use object-oriented programming actively.

Apply:

- Encapsulation
- Inheritance
- Polymorphism
- Composition
- Interfaces where useful

Use inheritance when there is a clear **is-a** relationship.

Prefer composition or interchangeable behavior objects when functionality can vary independently.

Do not maximize the number of classes or design patterns.

Avoid unnecessary inheritance depth, dependency injection, factories, or abstractions unless they provide a clear benefit.

Where practical, separate configuration/data from runtime behavior.

Examples:

- Turret stats should not need to be hard-coded into every turret class.
- Enemy stats should not need to be hard-coded into every enemy class.
- Stage/wave data should eventually be separate from spawn execution logic.

Refer to:

- `Tower.md` for turret architecture.
- `enemy.md` for enemy architecture.

---

## 11. Development Process

Before writing gameplay code:

1. Propose the Unity project structure.
2. List required GameObjects.
3. List required C# scripts/components.
4. Explain the responsibility of each script.
5. Explain how the main systems communicate.
6. Explain which data is configuration and which part is runtime logic.
7. Explain the proposed scene organization.

Then implement the minimal playable prototype.

Possible script names may include:

```text
GameManager.cs
GameStateManager.cs
ResourceManager.cs
EnemySpawner.cs
BaseController.cs
StageSelectionUI.cs
MainMenuUI.cs
PauseMenuUI.cs
SettingsUI.cs
```

These names are only suggestions.

Use more appropriate names if the architecture requires them, and briefly explain the reason.

### Priority

1. Functional gameplay
2. Clear responsibilities
3. Extensibility
4. Simplicity
5. Visual quality
