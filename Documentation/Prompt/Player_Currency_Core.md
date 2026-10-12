# Player Currency Core Specification

## Purpose

Introduce one authoritative persistent currency system for long-term progression:

```text
PlayerCurrency
├── Gold
└── Candy
```

Both currencies must survive scene changes, Retry, Game Over, application shutdown, and application restart. They will later be consumed by upgrade and shop systems.

## Core API

Expose controlled operations such as:

```text
AddGold(int amount)
AddCandy(int amount)
TrySpendGold(int amount)
TrySpendCandy(int amount)
```

Exact names may differ.

Do not allow unrelated gameplay systems to mutate raw currency fields directly.

Currency values must never become negative.

## Persistence

Reuse the project's existing persistence layer if suitable. Otherwise, persist a small serializable progression model under:

```text
Application.persistentDataPath
```

A JSON implementation is acceptable.

Conceptually:

```text
PlayerProgressionSaveData
├── Version
├── Gold
├── Candy
└── existing/future progression data
```

Save immediately after a committed currency change.

If saving fails:

- do not crash
- keep the current in-memory value
- log a clear error
- do not claim persistence succeeded

## Gold Role

Gold is primarily a Stage Clear reward.

Future purpose:

```text
Replay completed stages
-> accumulate Gold
-> spend Gold on persistent upgrades
```

Gold is granted only by the Stage Victory reward flow defined in the Stage Clear specification.

## Candy Role

Candy is primarily an Enemy-drop reward.

Candy is committed immediately when a successful drop roll occurs.

Once awarded, Candy remains owned even if the current Stage later ends in Game Over.

## UI Integration

Expose change notifications so HUD/Lobby UI can update without owning the currency data.

Conceptually:

```text
PlayerCurrency
-> CurrencyChanged
-> UI refresh
```

Keep presentation separate from persistence/domain logic.

## Current Scope

Implement now:

- Gold
- Candy
- controlled add/spend operations
- persistence
- non-negative validation
- change notifications

Do not implement yet:

- upgrade shop
- upgrade effects
- Candy shop
- currency exchange
- cloud sync
- encryption
- premium currency

## Verification

Verify at minimum:

1. Gold can be added.
2. Candy can be added.
3. Invalid spending cannot make currency negative.
4. Gold and Candy survive scene changes.
5. Gold and Candy survive Retry.
6. Candy survives Game Over.
7. Gold and Candy survive full application restart.
8. UI can read/observe the authoritative currency state.
9. Save failure does not crash gameplay.

## Implementation Report

Report:

1. Existing persistence components reused.
2. Files created or modified.
3. Authoritative currency class/service.
4. Save-data format and location.
5. Notification mechanism.
6. Automated tests performed.
7. Manual restart tests actually performed.
8. Anything unverified.
