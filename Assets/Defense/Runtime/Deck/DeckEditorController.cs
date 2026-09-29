using System.Collections.Generic;
namespace Defense {
public sealed class DeckEditorController {
    private readonly PlayerDeck deck;
    public IReadOnlyList<TurretDefinition> Available { get; }
    public DeckEditorController(PlayerDeck deck, IReadOnlyList<TurretDefinition> available) {
        this.deck = deck; Available = available;
    }
    public bool Replace(int slot, TurretDefinition tower) {
        bool available = false;
        foreach (var item in Available) if (item == tower) available = true;
        return available && deck.Replace(slot,tower);
    }
}
}
