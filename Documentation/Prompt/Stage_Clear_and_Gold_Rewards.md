# Stage Clear and Gold Reward Specification

## Purpose

Implement an actual Stage Clear / Victory flow and fixed Gold rewards.

The project already has progression support through an existing `CompleteActiveStage()`-style operation, but gameplay currently lacks a complete victory pipeline.

This specification adds:

- finite-stage completion
- clear-condition evaluation
- explicit Victory state
- first-clear progression handling
- repeat-clear support
- fixed Gold reward on every successful clear
- simple Victory UI

## 1. Victory Flow

Use one explicit flow:

```text
Stage gameplay
-> clear condition becomes true
-> enter Victory exactly once
-> update progression
-> grant fixed Gold reward
-> show Victory UI
```

Do not scatter this logic across unrelated Update loops.

## 2. Victory State

The gameplay state model should support:

```text
Playing
Paused
GameOver
Victory
```

Victory must be terminal for the current run and mutually exclusive with Game Over.

When Victory begins:

- stop further enemy spawning
- prevent duplicate reward processing
- prevent duplicate progression processing
- show Victory UI
- stop normal gameplay progression as appropriate

## 3. Generic Clear Condition

Prefer:

```text
SpawnerFinished == true
AND
AliveEnemyCount == 0
```

Do not hard-code `Kills == 5` into the global Stage Clear system.

The Test Stage will spawn exactly five enemies, which naturally makes defeating all five the practical clear condition.

## 4. Finite Spawning

The enemy spawner must support finite spawn schedules and expose whether all configured enemies have been spawned.

Conceptually:

```text
EnemySpawner
├── configured spawn schedule
├── IsFinishedSpawning
└── active enemy tracking/event integration
```

Keep the design extensible for future wave-based stages.

## 5. Alive Enemy Tracking

Track currently alive stage enemies reliably.

Prefer event-driven tracking rather than repeated scene-wide searches.

Enemy spawn:

```text
AliveEnemyCount += 1
```

Valid Enemy death/removal from the combat population:

```text
AliveEnemyCount -= 1
```

Ensure each Enemy changes the count exactly once.

## 6. Progression and Rewards Must Be Separate

Use an orchestration layer such as:

```text
StageVictoryController
├── StageProgression
│   └── CompleteActiveStage()
├── StageReward
│   └── AddGold(StageDefinition.ClearGoldReward)
└── Victory UI
```

Do not put Gold-award logic directly inside `CompleteActiveStage()`.

Progression and reward are separate responsibilities.

## 7. Repeat Clears

Stages may be replayed.

Every successful clear grants the configured Gold reward.

```text
First clear
-> mark completed
-> unlock next stage if applicable
-> grant Gold

Repeat clear
-> progression remains completed
-> no duplicate unlock side effects
-> grant Gold again
```

This intentionally supports farming earlier stages for long-term upgrades.

## 8. Idempotent Completion

Make progression completion safe to call repeatedly.

Preferred behavior:

```text
CompleteActiveStage()

if not previously completed:
    mark completed
    unlock next stage
else:
    leave progression unchanged
```

If the existing function already behaves this way, reuse it.

Otherwise, update or wrap it cleanly.

## 9. Stage Gold Reward

Each Stage should define:

```text
ClearGoldReward
```

On Victory:

```text
PlayerCurrency.AddGold(StageDefinition.ClearGoldReward)
```

The amount must be configurable per Stage.

Do not hard-code one global reward inside the Victory controller.

## 10. Reward Timing and Duplicate Protection

Grant Gold when Victory is committed.

Exactly one Gold reward may be granted per run.

Do not grant Gold:

- every frame while the condition remains true
- on Pause
- on Game Over
- merely because the Stage was completed in a prior run

## 11. Victory UI

Add a simple Victory UI if none exists.

At minimum display:

```text
Stage Clear
Gold Earned: <amount>
```

Navigation may include existing/relevant options such as:

- Replay
- Return to Lobby
- Next Stage

Keep UI presentation separate from progression and reward logic.

## 12. Replay Reset

Replaying must create a fresh run:

```text
Spawner reset
AliveEnemy tracking reset
Victory flag reset
GameOver flag reset
Runtime Card cycle reset
Elixir reset
Placed Towers reset
Active Skill Areas reset
```

Persistent Gold must not reset.

## 13. Game Over

Game Over must never grant the Stage Clear Gold reward.

Candy already acquired during the run remains persistent according to the Candy specification.

## 14. Current Scope

Implement now:

- finite stage completion
- Victory state
- clear-condition evaluation
- safe progression completion
- configurable fixed Gold reward
- repeat-clear Gold farming
- one-time reward protection
- simple Victory UI

Do not implement yet:

- star ratings
- bonus objectives
- time reward multipliers
- first-clear bonus reward
- difficulty reward multipliers
- upgrade shop

## 15. Verification

Verify at minimum:

1. A finite spawner can finish.
2. Victory does not occur while an Enemy remains alive.
3. Victory occurs after spawning is finished and all active Enemies are gone.
4. Victory processing occurs once.
5. First clear updates progression.
6. Repeat clear does not corrupt progression.
7. First clear grants Gold.
8. Repeat clear grants Gold again.
9. Gold is granted exactly once per run.
10. Game Over grants no clear Gold.
11. Victory UI appears.
12. Replay creates a fresh transient run state.
13. Gold persists after restart.

## 16. Implementation Report

Report:

1. Existing progression code reused.
2. Whether `CompleteActiveStage()` was changed, wrapped, or reused unchanged.
3. Files created or modified.
4. Clear-condition architecture.
5. Spawner completion mechanism.
6. Alive-enemy tracking.
7. Victory state integration.
8. Gold reward integration.
9. Repeat-clear behavior.
10. Automated tests.
11. Manual clear/replay tests actually performed.
12. Anything unverified.
