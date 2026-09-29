using System;
using System.Collections.Generic;
namespace Defense {
public sealed class PlayerDeck {
    public const int SlotCount = 6;
    private readonly TurretDefinition[] slots;
    private readonly IReadOnlyList<TurretDefinition> view;
    public IReadOnlyList<TurretDefinition> Slots => view;
    public event Action Changed;
    public PlayerDeck(IReadOnlyList<TurretDefinition> initial) {
        if (initial == null || initial.Count != SlotCount) throw new ArgumentException("PlayerDeck requires six entries.");
        slots = new TurretDefinition[SlotCount];
        for (int i=0;i<SlotCount;i++) slots[i] = initial[i] != null ? initial[i] : throw new ArgumentException("Deck entries cannot be null.");
        view = Array.AsReadOnly(slots);
    }
    internal bool Replace(int slot, TurretDefinition tower) {
        if (slot < 0 || slot >= SlotCount || tower == null) return false;
        if (slots[slot] == tower) return true;
        slots[slot] = tower; Changed?.Invoke(); return true;
    }
}
}
