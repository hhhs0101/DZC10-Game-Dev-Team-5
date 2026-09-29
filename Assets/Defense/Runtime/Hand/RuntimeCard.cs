namespace Defense {
public sealed class RuntimeCard {
    // Original persistent slot for tracing; tower definitions are unique and this is not a draw priority.
    public int SourceSlot { get; }
    public TurretDefinition Definition { get; }
    public RuntimeCard(int sourceSlot, TurretDefinition definition) { SourceSlot = sourceSlot; Definition = definition; }
}
}
