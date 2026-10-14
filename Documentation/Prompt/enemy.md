# Enemy System Specification

## 1. Goal

Design the enemy system as an extensible object-oriented subsystem.

The current prototype only requires one basic enemy type, but additional enemy types should be addable later without rewriting the core Enemy system.

Use inheritance when there is a clear `is-a` relationship.

Use composition/components/interfaces for characteristics that may vary independently.

Do not over-engineer the current prototype.

---

## 2. Enemy Inheritance Structure

Use a common Enemy abstraction.

Recommended conceptual structure:

```text
Enemy
├── BasicEnemy
├── FastEnemy
├── TankEnemy
├── FlyingEnemy
└── FutureEnemy...
```

The base `Enemy` abstraction contains state and behavior shared by all enemy types.

Future enemy classes should inherit from `Enemy` only when they are genuinely specialized enemy types.

---

## 3. Common Enemy Properties

The base Enemy system should support common properties such as:

- Health
- Movement Speed
- Base Damage

Future properties may include:

- Armor
- Enemy category/type
- Flying status
- Status resistance
- Regeneration
- Special ability data

Do not implement these advanced properties unless required by the current prototype.

Where practical, separate enemy configuration/data from runtime behavior.

---

## 4. Common Enemy Behavior

The base Enemy system should handle common behavior such as:

- Following the assigned path
- Moving toward the next waypoint
- Receiving damage
- Tracking current HP
- Dying when HP reaches 0
- Reaching the Base
- Applying Base Damage
- Removing itself after death or after reaching the Base

Specific enemy subclasses should override or extend behavior only where required.

---

## 5. Current Basic Enemy

Initially implement only one playable enemy type:

`BasicEnemy`

The Basic Enemy should:

- Spawn from the test spawner.
- Follow the predefined stage path.
- Move using its Movement Speed.
- Receive damage from turrets.
- Die and be removed when HP reaches 0.
- Damage the Base when it reaches the end of the path.
- Be removed after reaching the Base.

The Basic Enemy should use the shared Enemy implementation rather than duplicate common logic.

---

## 6. Path Movement

Enemies move automatically along a predefined path.

The path may be represented using:

- Waypoints
- Path nodes
- Another simple Unity-friendly path representation

The implementation should be simple for the current prototype.

Keep path-following responsibility reasonably separate from unrelated systems.

The enemy should not directly contain stage/wave configuration logic.

---

## 7. Damage and Death

When an enemy receives damage:

```text
Current HP -= Damage
```

If:

```text
Current HP <= 0
```

the enemy dies and is removed.

Do not implement complex death animations or effects yet.

The system should later allow future additions such as:

- Death animation
- Resource reward
- Drop items
- On-death effects
- Explosion
- Enemy splitting

without rewriting the entire Enemy base system.

---

## 8. Reaching the Base

When an enemy reaches the final point of the path:

1. Apply its Base Damage to the player's Base.
2. Remove the enemy from the stage.

The Enemy should communicate the event through a clean interface or game-system responsibility rather than directly manipulating unrelated UI or global state where possible.

---

## 9. Future Enemy Types

Future enemy types may include:

### FastEnemy
Possible characteristics:

- Higher Movement Speed
- Lower Health

### TankEnemy
Possible characteristics:

- Higher Health
- Lower Movement Speed
- Future armor behavior

### FlyingEnemy
Possible characteristics:

- Classified as a flying target
- May interact differently with some turret targeting rules
- May later use different path or movement restrictions

Do not implement these enemy types yet unless explicitly requested.

The current architecture should only make them straightforward to add later.

---

## 10. Independent Enemy Characteristics

Do not represent every enemy characteristic through inheritance.

For example, future combinations might include:

- Flying + Armored
- Fast + Regenerating
- Tank + Status Resistant
- Flying + Shielded

Creating a subclass for every combination would create an unnecessarily complex inheritance tree.

Characteristics that can vary independently should be suitable for composition/components/interfaces.

Examples:

- Flying capability
- Armor
- Regeneration
- Status resistance
- Special abilities
- Different movement behaviors

Possible conceptual abstractions:

```text
IEnemyAbility
IArmorBehavior
IMovementBehavior
IStatusResistance
```

Do not implement these abstractions prematurely.

Only introduce them when they provide a clear benefit.

---

## 11. Interaction with Turret Targeting

The Enemy system should expose enough information for turret targeting logic to evaluate valid targets.

For the current prototype, the Basic Turret only requires enemy position and alive/valid state.

Future targeting rules may need information such as:

- Current HP
- Maximum HP
- Flying status
- Enemy category
- Path progress

Avoid making Turret code depend directly on specific concrete enemy subclasses where possible.

For example, a future AntiAirTurret should not need hard-coded checks such as:

```text
if enemy is FlyingEnemySubclassX
```

if a cleaner enemy capability/category representation can be used.

---

## 12. Spawn System Relationship

Enemy classes should not be responsible for wave scheduling.

Keep responsibilities separated:

```text
Stage / Wave Data
    -> EnemySpawner
        -> Enemy instance
            -> Path movement / combat / Base interaction
```

For the current prototype, the `EnemySpawner` may periodically spawn `BasicEnemy`.

Later it should be possible for the spawner to create different enemy types based on stage/wave definitions.

---

## 13. OOP Requirements

Use the enemy system to demonstrate meaningful OOP design.

### Encapsulation
Enemy health, death state, and internal runtime behavior should be controlled by the Enemy system.

### Inheritance
Specific enemy types may inherit from the common Enemy abstraction.

### Polymorphism
Other game systems should be able to work with different enemy subclasses through the common Enemy abstraction where appropriate.

### Composition
Independently variable abilities and characteristics should not automatically create large inheritance hierarchies.

### Extensibility
Adding a new enemy type should not require rewriting the core movement, damage, or Base interaction logic.

---

## 14. Suggested Responsibilities

Possible classes/components may include:

```text
Enemy
- Shared enemy runtime behavior

BasicEnemy
- Current concrete enemy type

EnemyDefinition / EnemyData
- Enemy configuration values

EnemyHealth
- Optional separate health responsibility if useful

EnemyPathFollower
- Optional separate movement/path responsibility

EnemySpawner
- Creates enemy instances for the current stage

Path / WaypointPath
- Defines the route enemies follow
```

These names are suggestions only.

Use names appropriate to the final architecture.
