using System.Collections.Generic;
namespace Defense {
public sealed class DeckEditorController {
    private readonly PlayerDeck deck;
    private readonly IReadOnlyList<TurretDefinition> catalog;
    // Computed from authoritative definitions and deck on every read; no independent inventory.
    public IReadOnlyList<TurretDefinition> Available {
        get {
            var available = new List<TurretDefinition>();
            var unique = new HashSet<TurretDefinition>();
            foreach (var tower in catalog)
                if (tower != null && !deck.Contains(tower) && unique.Add(tower)) available.Add(tower);
            return available.AsReadOnly();
        }
    }
    public DeckEditorController(PlayerDeck deck, IReadOnlyList<TurretDefinition> available) {
        this.deck = deck; catalog = available;
    }
    public bool Replace(int slot, TurretDefinition tower) {
        foreach (var item in Available) if (item == tower) return deck.Replace(slot,tower);
        return false;
    }
}
}
