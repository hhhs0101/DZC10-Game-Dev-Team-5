using System;
using System.Collections.Generic;
namespace Defense {
public sealed class PlayerDeck {
    public const int SlotCount = 6; // Maximum deck size.
    public const int MinimumSlotCount = 3;
    private readonly CardDefinition[] slots;
    private readonly IReadOnlyList<CardDefinition> view;
    public IReadOnlyList<CardDefinition> Slots => view;
    public const string MinimumTowerMessage = "Your deck must contain at least one tower card.";
    public event Action Changed;
    public event Action<string> Rejected;
    public PlayerDeck(IReadOnlyList<CardDefinition> initial) {
        if (initial == null || (initial.Count < MinimumSlotCount || initial.Count > SlotCount)) throw new ArgumentException("PlayerDeck requires three to six entries.");
        slots = new CardDefinition[initial.Count];
        for (int i=0;i<slots.Length;i++) slots[i] = initial[i] != null ? initial[i] : throw new ArgumentException("Deck entries cannot be null.");
        if (new HashSet<CardDefinition>(slots).Count != slots.Length)
            throw new ArgumentException("PlayerDeck requires unique card definitions.");
        if (!Array.Exists(slots, card => card is TowerCardDefinition)) throw new ArgumentException(MinimumTowerMessage);
        view = Array.AsReadOnly(slots);
    }
    public bool Contains(CardDefinition tower) => Array.IndexOf(slots,tower) >= 0;
    internal bool Replace(int slot, CardDefinition tower) {
        if (slot < 0 || slot >= slots.Length || tower == null) return false;
        if (slots[slot] == tower) return true;
        if (Contains(tower)) return false;
        if (slots[slot] is TowerCardDefinition && !(tower is TowerCardDefinition) && Array.FindAll(slots, card => card is TowerCardDefinition).Length == 1) { Rejected?.Invoke(MinimumTowerMessage); return false; }
        slots[slot] = tower; Changed?.Invoke(); return true;
    }
}
}
