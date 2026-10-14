# Repeatable Test Stage Specification

## Purpose

Add a short repeatable Stage for quickly testing:

- Stage Clear
- Victory state
- fixed Gold rewards
- repeat-clear Gold farming
- Candy drops
- persistent currencies
- replay/reset behavior

The Test Stage should be intentionally short and development-friendly.

## 1. Test Stage Goal

Configure the Stage with:

```text
Total Enemies = 5
Finite Spawn Schedule = true
Repeatable = true
```

The practical result should be:

```text
Defeat all 5 configured Enemies
-> Stage Clear
```

Use the general Stage Clear system rather than a special Test-Stage-only completion function.

## 2. Finite Spawn Schedule

The Test Stage must stop spawning after five Enemies.

Example:

```text
Spawn Enemy 1
Spawn Enemy 2
Spawn Enemy 3
Spawn Enemy 4
Spawn Enemy 5
SpawnerFinished = true
```

No sixth Enemy may spawn.

## 3. Clear Condition

Use the shared clear condition:

```text
SpawnerFinished == true
AND
AliveEnemyCount == 0
```

Do not clear the Stage merely because five spawn requests have been issued if an Enemy is still alive.

## 4. Fast Test Configuration

Keep the Stage short enough for rapid iteration.

Use development-friendly values for:

- spawn interval
- Enemy health
- Enemy movement speed
- path length

Do not rebalance the entire game around this Test Stage.

## 5. Gold Reward

Assign a configurable fixed Gold reward.

Example development value:

```text
ClearGoldReward = 50
```

The exact value may be changed later.

Every successful clear awards this reward.

## 6. Repeatability

The Stage must be replayable.

Example:

```text
Clear Test Stage
-> receive Gold
-> replay
-> clear again
-> receive Gold again
```

Repeated clears must not:

- duplicate first-clear progression side effects
- corrupt unlocked stages
- reset persistent currencies
- reset Deck presets

## 7. Candy Testing

The five test Enemies should use Enemy definitions that make Candy testing practical.

For development, it is acceptable to configure test Enemy definitions with clearly observable probabilities such as:

```text
0%
50%
100%
```

Do not hard-code these values into the Candy system.

The probabilities must remain Enemy-definition data.

## 8. Failure Testing

The Test Stage must also support Game Over verification.

Required reward behavior:

```text
Candy acquired before failure
-> remains persistent

Gold clear reward
-> not granted on failure
```

This distinction must remain explicit.

## 9. Replay Reset

Replay must reset transient gameplay state:

```text
Spawner state
AliveEnemy tracking
Victory state
GameOver state
Runtime Card cycle
Elixir
Placed Towers
Active Skill Areas
Other transient combat objects
```

Replay must not reset:

```text
Gold
Candy
Completed stages
Unlocked stages
Deck presets
```

## 10. Access

Make the Test Stage easy to launch using the existing Stage selection architecture or another clean development-accessible route.

Prefer integration with the current Stage system rather than a parallel testing-only gameplay framework.

## 11. Current Scope

Implement now:

- one finite five-Enemy Test Stage
- clear after all five are defeated
- fixed configurable Gold reward
- repeatability
- Candy-drop compatibility
- failure testing
- correct replay reset

Do not implement yet:

- advanced wave editor
- procedural waves
- boss wave
- endless mode
- score ranking
- time attack
- difficulty selection

## 12. Verification

Verify at minimum:

1. Exactly five Enemies are spawned.
2. No sixth Enemy appears.
3. Stage does not clear while an Enemy remains alive.
4. Stage clears after all five are defeated.
5. Gold is awarded once on Victory.
6. Replaying and clearing again awards Gold again.
7. Candy drops follow Enemy configuration.
8. Candy remains after a failed run.
9. Failed run grants no clear Gold.
10. Replay resets transient Stage state.
11. Persistent Gold and Candy remain intact between runs.
12. Deck presets remain intact between runs.

## 13. Implementation Report

Report:

1. Scene/configuration added or modified.
2. Five-Enemy spawn configuration.
3. Clear-condition integration.
4. Gold test reward value.
5. Candy test configuration.
6. Replay/reset behavior.
7. Automated tests.
8. Manual clear/replay/failure tests actually performed.
9. Anything unverified.
