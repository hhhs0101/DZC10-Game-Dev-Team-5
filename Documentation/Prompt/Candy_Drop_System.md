# Candy Drop System Specification

## Purpose

Implement Candy as a persistent Enemy-drop currency.

Candy should:

- be rolled when an Enemy dies
- use a drop chance configured per Enemy type
- optionally use a configurable drop amount per Enemy type
- be awarded automatically when the drop roll succeeds
- remain owned even if the current Stage later ends in Game Over
- persist across application restart

Candy is a long-term progression resource and will be used by future systems.

## 1. Enemy-Specific Candy Drop Data

Add Candy drop configuration to the Enemy definition/data layer.

Conceptually:

```text
EnemyDefinition
├── MaxHealth
├── MovementSpeed
├── existing combat attributes
├── CandyDropChance
└── CandyDropAmount
```

Example development configuration:

```text
Basic Enemy
CandyDropChance = 0.10
CandyDropAmount = 1

Fast Enemy
CandyDropChance = 0.15
CandyDropAmount = 1

Elite Enemy
CandyDropChance = 0.40
CandyDropAmount = 2
```

Exact balance values must remain configurable.

## 2. Probability Representation

Use normalized probability values:

```text
0.0 = 0%
1.0 = 100%
```

Validate or clamp:

```text
0 <= CandyDropChance <= 1
CandyDropAmount >= 0
```

## 3. Separate Enemy Combat from Reward Logic

Do not make the Enemy runtime directly own PlayerCurrency.

Prefer:

```text
Enemy dies
-> death event includes EnemyDefinition
-> CandyDropSystem handles reward roll
-> PlayerCurrency.AddCandy(...)
```

This keeps Enemy combat logic independent from long-term reward logic.

## 4. Drop Roll

On one valid Enemy death:

```text
Random roll
-> compare with CandyDropChance
```

Success:

```text
PlayerCurrency.AddCandy(CandyDropAmount)
```

Failure:

```text
No Candy awarded
```

The drop roll must happen exactly once per Enemy death.

## 5. Automatic Acquisition

Candy does not require manual pickup.

Do not require:

- mouse click
- pickup collision
- collection radius
- player interaction

Required flow:

```text
Enemy dies
-> Candy roll succeeds
-> Candy is immediately added
-> persistent state is saved
```

## 6. Candy Is Retained After Failure

Candy is permanently committed as soon as a successful drop is awarded.

Example:

```text
Enemy 1 drops Candy
-> Candy +1

Enemy 2 drops Candy
-> Candy +1

Stage ends in Game Over
-> acquired Candy remains
```

Do not maintain a run-only Candy wallet that is discarded on failure.

## 7. Visual Drop Feedback

A visual Candy effect may be added, but it is presentation only.

Recommended flow:

```text
Enemy dies
-> drop succeeds
-> Candy reward is immediately committed
-> temporary Candy visual appears at Enemy position
-> optional pop/bounce
-> optional movement toward Candy UI
-> visual fades/despawns
```

The Candy must already belong to the player before this animation completes.

The visual object must not own or delay the reward.

## 8. Portable Visual Architecture

Keep the visual replaceable.

The reward system must not depend on:

- a specific model
- a specific sprite
- a specific animation
- a physics pickup
- a particle system

The current prototype may use a primitive/icon or no visual at all.

## 9. Valid Death Semantics

Roll Candy only for a legitimate Enemy death according to the existing combat semantics.

Do not roll more than once if multiple systems observe destruction.

If an Enemy reaches the Base and is removed without being killed, do not automatically treat arbitrary object destruction as a Candy-producing kill.

Reuse the project's existing death/kill semantics where possible.

## 10. Persistence

Use the shared Player Currency system.

Successful drop:

```text
PlayerCurrency.AddCandy(amount)
-> persistent save
```

Do not store Candy only in the active Stage scene.

## 11. UI

Allow relevant UI to display persistent Candy total.

Possible locations:

- gameplay HUD
- Lobby
- future Upgrade menu

UI must read from the authoritative PlayerCurrency state.

## 12. Current Scope

Implement now:

- Enemy-specific CandyDropChance
- Enemy-specific CandyDropAmount
- one roll per valid Enemy death
- immediate automatic acquisition
- failure retention
- persistence
- optional lightweight visual feedback
- UI counter integration where practical

Do not implement yet:

- manual Candy pickup
- click-to-collect
- pickup collision
- magnet collection
- multiple Candy item types
- Candy rarity
- Candy shop
- Candy upgrades
- run-only Candy

## 13. Verification

Verify at minimum:

1. Different Enemy types can define different drop chances.
2. Different Enemy types can define different drop amounts.
3. Invalid chance values are prevented or safely handled.
4. One valid Enemy death produces one roll.
5. Successful roll adds Candy immediately.
6. Failed roll adds nothing.
7. Candy acquisition requires no click.
8. Candy remains after Game Over.
9. Candy remains after returning to Lobby.
10. Candy remains after application restart.
11. Optional Candy visual does not control ownership.
12. Removing/interruption of the visual cannot remove the reward.
13. Enemy combat logic remains separated from reward logic.

## 14. Implementation Report

Report:

1. EnemyDefinition fields added or reused.
2. Files created or modified.
3. Enemy death event flow.
4. CandyDropSystem architecture.
5. Drop-roll implementation.
6. Persistence integration.
7. Failure-retention behavior.
8. Visual feedback implementation, if any.
9. Automated tests.
10. Manual drop tests actually performed.
11. Anything unverified.
