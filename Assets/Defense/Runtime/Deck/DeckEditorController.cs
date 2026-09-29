using System.Collections.Generic;
namespace Defense {
public sealed class DeckEditorController {
    private readonly PlayerDeck deck;
    private readonly IReadOnlyList<CardDefinition> catalog;
    // Computed from authoritative definitions and deck on every read; no independent inventory.
    public IReadOnlyList<CardDefinition> Available {
        get {
            var available = new List<CardDefinition>();
            var unique = new HashSet<CardDefinition>();
            foreach (var tower in catalog)
                if (tower != null && !deck.Contains(tower) && unique.Add(tower)) available.Add(tower);
            return available.AsReadOnly();
        }
    }
    public DeckEditorController(PlayerDeck deck, IReadOnlyList<CardDefinition> available) {
        this.deck = deck; catalog = available;
    }
    public bool Replace(int slot, CardDefinition tower) {
        foreach (var item in Available) if (item == tower) return deck.Replace(slot,tower);
        return false;
    }
}
}
