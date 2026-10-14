# Tower System Specification

## 1. Goal

Design the turret system as an extensible object-oriented subsystem.

The current prototype only requires one playable basic turret, but additional turret types must be addable later without rewriting the entire turret system.

Use inheritance where there is a clear `is-a` relationship and composition/strategy-style behavior where functionality may vary independently.

Do not over-engineer the system.

---

## 2. Turret Inheritance Structure

Use a common Turret abstraction.

Recommended conceptual structure:

```text
Turret
├── BasicTurret
├── SniperTurret
├── SplashTurret
├── AntiAirTurret
└── FutureTurret...
```

`Turret` should be the common base abstraction.

Do **not** use `BasicTurret` as the parent of all future turret types unless there is a genuine inheritance relationship.

For example:

`SniperTurret is a Turret`

is valid.

`SniperTurret is a BasicTurret`

is not necessarily valid.

---

## 3. Common Turret Properties

The Turret base abstraction should support common properties such as:

- Damage
- Attack Range
- Attack Cooldown / Attack Speed
- Cost
- Targeting Rule

Where practical, separate turret configuration from runtime behavior.

The architecture should make it possible to define future turret types by changing data/configuration rather than duplicating large amounts of code.

---

## 4. Common Turret Behavior

The base Turret system should handle common behavior such as:

- Detecting enemies within attack range
- Acquiring a valid target
- Tracking attack cooldown
- Attacking when ready
- Reacquiring a target if the current target becomes invalid
- Common lifecycle/state logic

If the current target:

- Dies
- Leaves attack range
- Becomes invalid for the turret's targeting rule

the turret should automatically search for a new valid target.

---

## 5. Current Basic Turret

Initially implement only one playable turret type:

`BasicTurret`

Its default targeting behavior is:

`Closest enemy`

For the current implementation, `Closest` means the enemy with the smallest physical distance from the turret.

The Basic Turret should use the common Turret system rather than reimplementing common attack logic.

---

## 6. Attack System

For the initial prototype, the turret may apply damage directly to the selected enemy.

Projectile visuals are not required.

Conceptually:

```text
Turret acquires target
    -> Attack cooldown ready
        -> Apply damage directly
```

However, keep the combat architecture extensible so projectile-based attacks can be added later.

Future examples:

- Bullet projectile
- Missile projectile
- Area-of-effect attack
- Beam attack
- Chain attack

Do not implement these advanced attack types yet.

---

## 7. Targeting Rules

Targeting behavior must be extensible.

Future turrets may use different targeting rules such as:

1. Closest enemy
2. Enemy with the highest HP within range
3. Flying enemy
4. Enemy furthest along the path
5. Other custom targeting rules

Only the default closest-target rule must be implemented now.

Do not strongly couple all target-selection logic directly into each turret subclass.

Prefer a reusable targeting abstraction such as:

```text
ITargetingStrategy
├── ClosestTargeting
├── HighestHealthTargeting
├── FlyingEnemyTargeting
└── FutureTargeting...
```

The exact names and implementation may differ.

The purpose is to allow:

```text
BasicTurret
    -> ClosestTargeting

AntiAirTurret
    -> FlyingEnemyTargeting
```

without duplicating the entire turret implementation.

Do not create unnecessary strategy classes for behaviors that are not yet useful.

---

## 8. Tower Placement

Turrets may only be placed on predefined Tower Placement Slots.

The basic placement flow is:

1. Select a turret from the Build/Create UI.
2. Click a predefined Tower Placement Slot.
3. Validate the slot.
4. Validate resource cost.
5. Place the turret if all conditions are satisfied.
6. Deduct the turret cost.

A placement slot should at least support:

- Available
- Occupied

Do not allow multiple turrets to occupy the same slot.

---

## 9. Placement Errors

If the selected slot already contains a turret, display:

`A turret is already placed here.`

If the player tries to place a turret somewhere that is not a valid placement location, display:

`You cannot place a turret here.`

If the player does not have enough resource, display:

`Not enough resources.`

No resource should be consumed if placement fails.

---

## 10. Build/Create UI

The player selects available turret types through a simple build/create interface.

For the current prototype:

- Only `BasicTurret` is available.
- Use placeholder UI.
- Do not implement a complex inventory or unlock system.

The current meaning of "available turret" is a turret type that can be built, not a limited-count inventory item.

Future systems may add:

- Unlockable turrets
- Limited turret inventory
- Upgrade trees
- Turret selling
- Turret replacement

Do not implement these yet.

---

## 11. Future Turret Extension

The system should allow future turret types to differ through combinations of:

- Stats
- Targeting behavior
- Attack behavior
- Projectile type
- Area-of-effect behavior
- Status effects
- Upgrade rules
- Valid enemy categories

Avoid creating a deep inheritance tree for every possible combination.

For example, avoid structures such as:

```text
ClosestFlyingSplashTurret
HighestHpFlyingSplashTurret
ClosestGroundSplashTurret
...
```

Use inheritance for stable type identity and composition for independently variable behaviors.

---

## 12. OOP Requirements

Use the turret system to demonstrate meaningful OOP design.

Apply:

### Encapsulation
Internal attack state and cooldown state should be managed by the turret system rather than modified arbitrarily by unrelated classes.

### Inheritance
Specific turret types may inherit from the common Turret abstraction.

### Polymorphism
The game should be able to treat different turret subclasses as Turret objects while allowing specialized behavior.

### Composition
Targeting and other independently variable behavior should preferably be replaceable without creating excessive subclasses.

### Extensibility
Adding a new turret type should not require rewriting existing turret logic.

---

## 13. Suggested Responsibilities

Possible responsibilities may include:

```text
Turret
- Shared turret runtime behavior

BasicTurret
- Current concrete turret type

TurretDefinition / TurretData
- Turret configuration values

ITargetingStrategy
- Target selection contract

ClosestTargeting
- Current default targeting implementation

TowerPlacementSlot
- Valid placement location and occupancy

TurretPlacementController
- Player placement workflow

BuildMenuUI
- Turret selection UI
```

These names are suggestions only.

Use names appropriate to the final architecture.
