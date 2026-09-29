namespace Defense {
public sealed class RuntimeCard {
    // Slot identity distinguishes repeated definitions; it is not a draw priority.
    public int SourceSlot { get; }
    public TurretDefinition Definition { get; }
    public RuntimeCard(int sourceSlot, TurretDefinition definition) { SourceSlot = sourceSlot; Definition = definition; }
}
}
