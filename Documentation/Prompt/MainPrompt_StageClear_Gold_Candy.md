# Main Prompt — Implement Stage Clear, Gold, Candy, and Repeatable Test Stage

Read the existing project specifications and inspect the current Unity project before modifying code.

Relevant existing systems include:

- PlayerSession / stage progression
- `CompleteActiveStage()` or equivalent progression operation
- Enemy spawning
- Enemy death
- Enemy definitions
- Game state
- Game Over
- Stage selection
- Retry
- Deck persistence
- any existing player/session persistence utilities

Then read and implement these specifications:

- `Player_Currency_Core.md`
- `Stage_Clear_and_Gold_Rewards.md`
- `Candy_Drop_System.md`
- `Repeatable_Test_Stage.md`

Treat those files as the source of truth for this task.

## 1. Inspect Before Editing

Identify:

- how completed/unlocked stages are stored
- how `CompleteActiveStage()` currently behaves
- whether progression is persistent or session-only
- how Enemies spawn
- whether the spawner supports finite counts
- how Enemy death is reported
- how Enemy definitions are represented
- how Game Over is represented
- how Retry resets gameplay
- whether a reusable persistence service already exists

Reuse working systems where possible.

Do not duplicate infrastructure unnecessarily.

## 2. Implement the Shared Currency Core First

Create or reuse one authoritative persistent system:

```text
PlayerCurrency
├── Gold
└── Candy
```

Required operations should conceptually include:

```text
AddGold
AddCandy
TrySpendGold
TrySpendCandy
```

Gold and Candy must survive a full application restart.

Use the current persistence architecture if suitable. Otherwise use a small JSON save under:

```text
Application.persistentDataPath
```

## 3. Implement Real Stage Victory

Implement an actual gameplay clear flow:

```text
Playing
-> clear condition satisfied
-> Victory
```

Preferred condition:

```text
SpawnerFinished == true
AND
AliveEnemyCount == 0
```

Victory must execute exactly once per run.

Game Over and Victory must be mutually exclusive.

## 4. Keep Progression and Rewards Separate

Use an orchestration flow such as:

```text
StageVictoryController
├── progression completion
├── Gold reward
└── Victory UI
```

Do not put Gold reward logic inside `CompleteActiveStage()`.

Make progression completion safe for repeat clears.

Preferred behavior:

```text
First clear
-> mark complete
-> unlock next stage

Repeat clear
-> progression remains unchanged
```

The Victory controller may call the progression completion operation on every clear if it is idempotent.

## 5. Gold Rules

Gold is awarded only on Stage Victory.

Each Stage defines:

```text
ClearGoldReward
```

Every successful clear, including repeat clears, grants the configured fixed amount.

This intentionally allows replaying older stages to farm Gold for future persistent upgrades.

Do not grant clear Gold on Game Over.

Persist Gold immediately after granting it.

## 6. Candy Rules

Candy is Enemy-drop currency.

Each Enemy definition/configuration defines:

```text
CandyDropChance
CandyDropAmount
```

On one valid Enemy death:

```text
Enemy death
-> CandyDropSystem
-> roll probability
-> success:
   PlayerCurrency.AddCandy(amount)
```

Keep reward logic separate from Enemy combat logic where practical.

Candy acquisition is automatic.

Do not require mouse clicking or manual pickup.

## 7. Candy Survives Failure

Candy is committed immediately when the drop roll succeeds.

Required:

```text
Enemy drops Candy
-> Candy added and persisted
-> player later reaches Game Over
-> Candy remains owned
```

Do not use a temporary Candy wallet that is discarded on failure.

## 8. Candy Visuals Are Non-Authoritative

If visual feedback is added:

```text
Enemy dies
-> reward already granted
-> temporary Candy visual appears
-> optional pop/bounce/fly/fade
-> visual removed
```

The visual must never control whether the Candy is owned.

Do not implement click-to-collect.

## 9. Add the Repeatable Five-Enemy Test Stage

Create a short Test Stage:

```text
Total Enemies = 5
Finite Spawn Schedule = true
Repeatable = true
```

The Test Stage clears when:

```text
all five have been spawned
AND
AliveEnemyCount == 0
```

Assign a configurable fixed Gold reward.

Make it easy to replay.

Use Enemy configurations that make Candy drop testing practical.

## 10. Replay Reset Rules

On Retry/Replay, reset transient state:

- spawner progress
- alive Enemies
- Victory state
- Game Over state
- runtime Hand/Card cycle
- Elixir
- placed Towers
- active Skill Areas
- transient combat objects

Do not reset:

- Gold
- Candy
- completed stages
- unlocked stages
- Deck presets

## 11. Reward Contract

Keep this distinction exact:

```text
Victory:
-> fixed Gold reward
-> Candy already earned remains

Game Over:
-> no clear Gold reward
-> Candy already earned remains
```

## 12. Persistence

Gold and Candy persistence is required now.

If stage completion/unlock is still session-only, inspect whether it can be cleanly integrated with the current save model.

Do not silently expand scope into a large progression rewrite.

If progression persistence remains unimplemented, report that explicitly.

## 13. Scope Control

Do not implement yet:

- upgrade shop
- upgrade effects
- Candy shop
- manual Candy pickup
- click-to-collect
- procedural waves
- endless mode
- bonus clear objectives
- reward multipliers
- item inventory
- currency exchange
- advanced Candy VFX

Focus on:

- real Stage Clear
- fixed Gold clear rewards
- persistent Gold/Candy
- Enemy-specific Candy drops
- Candy retention after failure
- repeatable five-Enemy Test Stage

## 14. Testing

Preserve existing tests and add focused tests where practical.

Verify at minimum:

### Stage Clear

- finite spawner finishes
- Victory waits until all Enemies are gone
- Victory executes once
- first clear updates progression
- repeat clear remains safe
- Game Over and Victory are mutually exclusive

### Gold

- Victory grants configured Gold once
- repeat clear grants Gold again
- Game Over grants no clear Gold
- Gold persists across restart

### Candy

- Enemy-specific drop chance is used
- Enemy-specific drop amount is used
- each valid Enemy death rolls once
- successful drop awards Candy immediately
- failed drop awards nothing
- Candy survives Game Over
- Candy survives restart
- Candy requires no manual click
- any Candy visual is presentation only

### Test Stage

- exactly five Enemies spawn
- no sixth Enemy appears
- defeating all five causes Victory
- repeat clear works
- transient state resets
- persistent currency does not reset

Do not claim full application restart or manual scene interaction was tested unless it was actually performed.

## 15. Final Report

After implementation, report:

1. Existing systems/classes reused.
2. Files created or modified.
3. Current behavior of `CompleteActiveStage()`.
4. Stage Clear architecture.
5. finite spawner architecture.
6. alive-Enemy tracking.
7. Victory state integration.
8. PlayerCurrency architecture.
9. Gold reward flow.
10. Candy drop flow.
11. Candy failure-retention behavior.
12. Test Stage configuration.
13. persistence implementation.
14. automated tests performed.
15. manual tests actually performed.
16. any remaining unverified behavior or limitations.
