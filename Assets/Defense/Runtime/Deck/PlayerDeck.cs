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
        if (new HashSet<TurretDefinition>(slots).Count != SlotCount)
            throw new ArgumentException("PlayerDeck requires six unique tower definitions.");
        view = Array.AsReadOnly(slots);
    }
    public bool Contains(TurretDefinition tower) => Array.IndexOf(slots,tower) >= 0;
    internal bool Replace(int slot, TurretDefinition tower) {
        if (slot < 0 || slot >= SlotCount || tower == null) return false;
        if (slots[slot] == tower) return true;
        if (Contains(tower)) return false;
        slots[slot] = tower; Changed?.Invoke(); return true;
    }
}
}
