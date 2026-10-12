using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// One owner for three independent existing PlayerDeck models; the active deck is a view of that ownership.
public sealed class DeckPresetManager : IDisposable {
    public const int PresetCount = 3;
    public const int SaveVersion = 1;
    private readonly PlayerDeck[] presets = new PlayerDeck[PresetCount];
    private readonly DeckPersistence persistence;
    public int ActivePresetIndex { get; private set; }
    public PlayerDeck ActiveDeck => presets[ActivePresetIndex];
    public bool LastSaveSucceeded { get; private set; } = true;
    public string SavePath => persistence.SavePath;
    public event Action Changed;
    public DeckPresetManager(IReadOnlyList<CardDefinition> defaults,IReadOnlyList<CardDefinition> cards,DeckPersistence storage,Action<string> logWarning = null) {
        persistence = storage;
        var warn = logWarning ?? (message=>Debug.LogWarning(message));
        var catalog = new CardCatalog(cards);
        // A valid authored fallback is required. Saved data is never allowed to bypass PlayerDeck validation.
        var fallback = new PlayerDeck(defaults);
        foreach (var card in fallback.Slots)
            if (!catalog.TryResolve(card.CardId,out var resolved) || resolved != card) throw new ArgumentException("Default deck has an unresolved or ambiguous Card ID: "+card.CardId);
        var data = persistence.Load();
        bool repair = data == null;
        if (data != null && data.Version != SaveVersion) {
            warn("Unsupported deck save version "+data.Version+"; restoring default presets."); data = null; repair = true;
        }
        if (data != null && (data.Presets == null || data.Presets.Length != PresetCount)) {
            warn("Deck save must contain exactly three presets; preserving valid entries and repairing missing entries."); repair = true;
        }
        for (int i=0;i<PresetCount;i++) {
            try {
                var ids = data?.Presets != null && i<data.Presets.Length ? data.Presets[i]?.CardIds : null;
                if (ids == null || ids.Length != PlayerDeck.SlotCount) throw new ArgumentException("Expected six Card IDs.");
                var resolved = new CardDefinition[PlayerDeck.SlotCount];
                for (int slot=0;slot<resolved.Length;slot++)
                    if (!catalog.TryResolve(ids[slot],out resolved[slot])) throw new ArgumentException("Missing or ambiguous Card ID: "+(ids[slot] ?? "<null>"));
                presets[i] = new PlayerDeck(resolved);
            } catch (ArgumentException exception) {
                presets[i] = new PlayerDeck(fallback.Slots); repair = true;
                if (data != null) warn("Recovering preset "+(i+1)+": "+exception.Message);
            }
            presets[i].Changed += OnDeckChanged;
        }
        ActivePresetIndex = data?.ActivePresetIndex ?? 0;
        if (ActivePresetIndex < 0 || ActivePresetIndex >= PresetCount) {
            warn("Invalid active preset index; selecting Preset 1."); ActivePresetIndex = 0; repair = true;
        }
        if (repair) Save();
    }
    public PlayerDeck GetPreset(int index) {
        if (index < 0 || index >= PresetCount) throw new ArgumentOutOfRangeException(nameof(index));
        return presets[index];
    }
    public bool SwitchPreset(int index) {
        if (index < 0 || index >= PresetCount) return false;
        if (index == ActivePresetIndex) return true;
        ActivePresetIndex = index; Save(); Changed?.Invoke(); return true;
    }
    private void OnDeckChanged() { Save(); Changed?.Invoke(); }
    private void Save() {
        var data = new DeckSaveData { Version = SaveVersion, ActivePresetIndex = ActivePresetIndex, Presets = new DeckPresetData[PresetCount] };
        for (int i=0;i<PresetCount;i++) {
            var ids = new string[PlayerDeck.SlotCount];
            for (int slot=0;slot<ids.Length;slot++) ids[slot] = presets[i].Slots[slot].CardId;
            data.Presets[i] = new DeckPresetData { CardIds = ids };
        }
        LastSaveSucceeded = persistence.Save(data);
    }
    public void Dispose() { foreach (var deck in presets) deck.Changed -= OnDeckChanged; }
}
}
