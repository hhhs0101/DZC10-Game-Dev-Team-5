# Main Prompt — Implement Deck Persistence and Three Deck Presets

Read the existing project specifications and inspect the current Unity implementation before editing code.

Pay particular attention to:

- `Deck.md`
- `HandCycle.md`
- `CardSystem.md`
- existing `PlayerDeck`
- Deck Editor / Deck Lobby UI
- Available Cards logic
- Stage initialization
- RuntimeCardCycle

Then read and implement:

- `Deck_Persistence_and_Presets.md`

Treat that specification as the source of truth.

## Primary Problems

### 1. Deck Reset After Restart

Current behavior:

```text
Edit Deck
-> close game
-> launch game again
-> Deck returns to default
```

Fix this with actual persistent storage.

Identify the root cause before changing code.

Do not merely stop default initialization if there is still no persistence layer.

### 2. Three Independent Deck Presets

Add exactly:

```text
Preset 1
Preset 2
Preset 3
```

Add Deck Lobby / Deck Editor buttons:

```text
프리셋 1
프리셋 2
프리셋 3
```

Pressing a button must immediately switch the Deck UI to that preset.

Each preset must persist independently across restarts.

## Inspect Before Editing

Identify the current implementation of:

- PlayerDeck
- default Deck initialization
- CardDefinition stable identity
- Deck validation
- Available Cards
- drag/drop replacement
- RuntimeCardCycle
- Stage-start Deck copy
- Retry
- Deck UI creation
- any existing save/persistence service

Reuse existing systems where possible.

Do not create a parallel Deck architecture unless necessary.

## Persistence Requirements

Prefer:

```text
Application.persistentDataPath
+ JSON
```

unless the project already contains a suitable persistence abstraction.

Persist stable Card IDs, not runtime references.

Conceptually:

```text
DeckSaveData
├── Version
├── ActivePresetIndex
└── Presets[3]
    └── CardIds[6]
```

Use:

```text
Version = 1
```

## Autosave

A successful valid Deck edit must:

```text
update active preset
-> save immediately
```

Rejected edits must not alter saved state.

No separate Save button is required.

## Preset Switching

Switching preset must:

```text
set ActivePresetIndex
-> expose target preset as current Deck
-> refresh Deck slots
-> refresh Available Cards
-> refresh selected preset visual
-> persist ActivePresetIndex
```

Do not reload the scene.

## Existing Deck Rules

Every preset must contain:

```text
exactly 6 Cards
all unique
at least 1 Tower Card
```

The existing final-Tower protection must remain.

Persistence loading must not bypass validation.

## First Launch

If no save exists:

```text
Preset 1 = Default Deck copy
Preset 2 = Default Deck copy
Preset 3 = Default Deck copy
ActivePreset = Preset 1
```

They must be independent records after initialization.

Save the initialized data.

## Gameplay Integration

The active preset is the source for a new Stage:

```text
Active Preset
-> current Deck
-> runtime copy
-> shuffle
-> Hand + UpcomingQueue
```

Do not save runtime shuffle/Hand state back into preset data.

Retry should reuse the same preset composition but create a fresh runtime cycle.

## Invalid Data Recovery

Validate each preset separately.

If one preset is invalid:

- preserve valid presets
- restore only invalid preset to Default Deck
- log why
- save repaired state

If a Card ID no longer resolves, treat that preset as invalid.

Do not crash.

## Architecture

Keep responsibilities separate.

A suitable design is:

```text
CardCatalog
-> stable ID resolution

DeckPresetManager
-> three presets
-> active preset
-> validation

DeckPersistence
-> JSON load/save

DeckEditorUI
-> preset buttons and visual refresh
```

Exact names may differ.

Do not put file I/O directly in UI Button callbacks.

Do not create multiple sources of truth for the active Deck.

## UI

Add:

```text
프리셋 1
프리셋 2
프리셋 3
```

The active preset must be visually distinguishable.

Switching must immediately update:

- six Deck slots
- Available Cards
- drag/drop state
- preset selected-state visual

## Testing

Preserve existing tests and add focused tests where practical.

Verify:

- edited Deck survives full restart
- three presets remain independent
- active preset persists
- preset switching updates Deck model/UI
- Available Cards refresh correctly
- uniqueness rule remains
- minimum one Tower rule remains
- Stage uses active preset
- RuntimeCardCycle does not mutate saved preset
- Retry does not reset preset
- invalid save data recovers safely
- missing Card IDs recover safely

If actual application restart is not automated, state that manual restart verification is still required.

Do not claim manual restart verification unless it was actually performed.

## Final Report

After implementation, report:

1. Root cause of Deck reset.
2. Files created/modified.
3. Persistence architecture.
4. Save file/data format.
5. Card ID strategy.
6. Preset architecture.
7. UI integration.
8. Stage/runtime integration.
9. Invalid-data recovery.
10. Automated tests.
11. Manual restart test, if actually performed.
12. Remaining limitations or unverified behavior.
