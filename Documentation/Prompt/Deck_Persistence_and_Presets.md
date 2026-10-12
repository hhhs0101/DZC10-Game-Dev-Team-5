# Deck Persistence and Preset System Specification

## Purpose

Fix the current Deck persistence bug and add a three-preset Deck system.

Current problem:

```text
Player edits Deck
-> Game runs correctly during the current session
-> Game is closed
-> Game is launched again
-> Deck is reset to the default Deck
```

Required behavior:

```text
Player edits Deck
-> Deck is saved persistently
-> Game is closed
-> Game is launched again
-> Saved Deck is restored
```

In addition, add three independent Deck presets:

```text
Preset 1
Preset 2
Preset 3
```

Each preset stores its own six-card Deck.

The player can switch between presets from the Deck Lobby / Deck Editor.

## 1. Preserve Existing Deck Rules

Do not change the current Deck validity rules.

Every preset must satisfy:

```text
Deck size = exactly 6 CardDefinitions
All cards are unique
TowerCard count >= 1
SkillCard count >= 0
```

Existing restrictions remain active:

- no duplicate Cards
- at least one Tower Card
- cards currently in the selected Deck are excluded from Available Cards
- invalid Deck edits are rejected
- replacing the final Tower Card with a Skill Card remains forbidden

The persistence system must not bypass these rules.

## 2. Persistent Deck Storage

Deck configuration must survive application shutdown and restart.

Do not rely only on runtime/static state.

Store Deck data in persistent application storage.

Recommended approach:

```text
Application.persistentDataPath
-> JSON save file
```

Use a small serializable save model.

Conceptually:

```text
DeckSaveData
├── Version
├── ActivePresetIndex
└── Presets[3]
    ├── Preset 1
    ├── Preset 2
    └── Preset 3
```

Each preset should store stable Card identifiers rather than runtime object references.

Example:

```text
DeckPresetData
├── PresetId
└── CardIds[6]
```

Do not serialize direct ScriptableObject object references into JSON.

## 3. Stable Card Identification

Persist Cards using stable Card IDs.

Example:

```text
tower_basic
tower_splash
skill_toy_throw
skill_slow_area
```

On load:

```text
Stored Card ID
-> Card registry/catalog
-> CardDefinition
```

Do not persist Deck membership based only on:

- array index
- Inspector ordering
- display name
- temporary runtime instance ID

If a stable ID system already exists, reuse it.

## 4. Save Timing

Use automatic saving.

Whenever a valid Deck modification is successfully committed:

```text
Deck edit succeeds
-> update currently selected preset
-> save persistent Deck data immediately
```

A separate Save button is not required.

Failed or rejected Deck edits must not modify saved data.

## 5. Loading on Application Start

When Deck data initializes:

```text
Game starts
-> load DeckSaveData
-> validate stored data
-> restore three presets
-> restore ActivePresetIndex
-> expose active preset as current PlayerDeck
```

The active preset should also survive restart.

## 6. First Launch / Missing Save File

If no Deck save file exists:

```text
Preset 1 = copy of current valid Default Deck
Preset 2 = copy of current valid Default Deck
Preset 3 = copy of current valid Default Deck
ActivePreset = Preset 1
```

These must become independent preset records after initialization.

Editing one preset must not mutate another.

Save the initial data immediately.

## 7. Preset Independence

Each preset must maintain its own six Card IDs.

Do not share mutable Card-list instances between presets.

Changing Preset 1 must not change Preset 2 or Preset 3.

## 8. Active Preset

Maintain:

```text
ActivePresetIndex = 0, 1, or 2
```

The active preset determines:

- Deck shown in the Deck Lobby / Deck Editor
- Cards excluded from Available Cards
- Deck used when entering a Stage
- Deck copied into RuntimeCardCycle

Switching presets must not mutate other presets.

## 9. Preset Buttons in Deck Lobby

Add three buttons:

```text
프리셋 1
프리셋 2
프리셋 3
```

When pressed:

```text
Click preset button
-> update ActivePresetIndex
-> load that preset as current Deck
-> refresh Deck slots
-> refresh Available Cards
-> refresh selected preset visual state
-> persist ActivePresetIndex
```

Do not require scene reload.

## 10. Preset Button Visual State

The active preset must be visually identifiable using a simple state such as:

- highlight
- border
- selected background
- pressed/disabled style

Keep this visual treatment replaceable.

## 11. Deck Editor Refresh

Preset switching must immediately refresh:

```text
Deck slots
Available Cards
Drag/drop state
Preset selected-state visual
Deck validation UI
```

Do not leave stale UI from the previous preset.

## 12. Runtime Stage Integration

When entering a Stage:

```text
Active Preset
-> current PlayerDeck
-> runtime copy
-> shuffle once
-> Hand[3]
-> UpcomingQueue[3]
```

Do not mutate persistent preset data during:

- runtime shuffle
- Hand cycling
- Card use
- Retry

Persist Deck composition only.

Do not persist runtime Hand, queue order, Elixir, placed Towers, Skill Areas, or stage state.

## 13. Retry Behavior

Retry must use the same active preset Deck composition:

```text
Retry
-> read active preset Deck
-> create fresh runtime Card cycle
-> shuffle according to existing rules
```

Do not reset or overwrite preset data.

## 14. Save Validation

On load, validate each preset:

```text
exactly 6 Card IDs
all IDs resolve to valid CardDefinitions
all Cards are unique
at least 1 Tower Card exists
```

If only one preset is invalid, preserve the valid presets and recover only the invalid one.

Recommended behavior:

```text
Preset 1 valid
Preset 2 invalid
Preset 3 valid

-> preserve 1
-> reset 2 to Default Deck
-> preserve 3
-> save repaired data
-> log warning
```

## 15. Missing / Renamed Card Handling

If a saved Card ID cannot be resolved:

- treat that preset as invalid
- recover only that preset to Default Deck
- log the missing Card ID
- do not crash

## 16. Save Version

Include:

```text
Version = 1
```

No complex migration system is required yet.

## 17. Responsibility Separation

Recommended architecture:

```text
CardCatalog / CardRegistry
-> resolves stable Card IDs

DeckPresetManager
-> owns three presets
-> active preset
-> validates preset changes

DeckPersistence
-> serialize / deserialize DeckSaveData
-> persistent file access

DeckEditorUI
-> renders active preset
-> sends switch/edit requests
-> refreshes visuals
```

Exact class names may differ.

Do not perform JSON file I/O directly inside Button callbacks.

## 18. Existing PlayerDeck Integration

Reuse existing PlayerDeck where practical.

Do not create two unrelated sources of truth for the active Deck.

A valid design may be:

```text
DeckPresetManager
-> active Deck
-> existing PlayerDeck API
```

or refactor PlayerDeck so it cleanly represents the selected preset.

## 19. Save Failure Handling

If writing the save file fails:

- do not crash
- keep current in-memory Deck state
- log a clear error
- do not pretend persistence succeeded

A simple safe-write strategy is preferred where practical:

```text
serialize
-> write temporary file
-> replace current save file
```

## 20. Scope

### Implement Now

- persistent Deck storage
- autosave after valid Deck edit
- persistent active preset selection
- exactly 3 presets
- `프리셋 1`, `프리셋 2`, `프리셋 3` buttons
- immediate UI refresh on preset switch
- independent Deck composition per preset
- load/save validation
- first-run initialization
- active preset used by Stage runtime

### Do Not Implement Yet

- more than 3 presets
- preset renaming
- custom preset names
- cloud saves
- account sync
- save encryption
- Deck import/export codes
- duplicate Cards
- inventory quantities
- preset-specific progression
- preset-specific Elixir
- preset-specific stage state

## 21. Verification

Verify at minimum:

### Persistence

1. Edit Preset 1.
2. Close the application.
3. Launch the application again.
4. Preset 1 still contains the edited Deck.
5. Active preset selection survives restart.
6. Valid Deck edits autosave.
7. Rejected Deck edits do not change saved data.
8. Missing save file initializes valid presets.
9. Invalid save data does not crash loading.

### Presets

10. Exactly three buttons exist:
    - `프리셋 1`
    - `프리셋 2`
    - `프리셋 3`
11. Pressing a preset button changes the Deck shown.
12. Each preset stores a different Deck independently.
13. Editing one preset does not change the others.
14. Available Cards refresh correctly after switching.
15. Active preset is visually identifiable.
16. Uniqueness rules remain enforced.
17. At least one Tower Card remains required.

### Gameplay Integration

18. Entering a Stage uses the active preset.
19. Runtime shuffle contains exactly the six Cards from that preset.
20. Runtime Hand cycling does not modify saved preset data.
21. Retry does not reset preset data.
22. Relaunching does not restore the old default Deck unless save data is missing or invalid.

## 22. Implementation Report

After implementation, report:

1. Root cause of the previous Deck reset behavior.
2. Existing Deck classes reused.
3. Files created or modified.
4. Persistent save location/strategy.
5. Save-data structure.
6. Stable Card ID resolution approach.
7. Preset-manager architecture.
8. Active-preset integration with PlayerDeck.
9. Deck Editor preset-button integration.
10. First-launch behavior.
11. Invalid-save recovery behavior.
12. Automated tests performed.
13. Manual restart/persistence checks actually performed.
14. Anything that remains unverified.

Do not claim persistence across application restarts was manually verified unless the application was actually closed and launched again during testing.
