namespace Defense {
public sealed class RuntimeCard {
    // Original persistent slot for tracing; card definitions are unique and this is not a draw priority.
    public int SourceSlot { get; }
    public CardDefinition Definition { get; }
    public RuntimeCard(int sourceSlot, CardDefinition definition) { SourceSlot = sourceSlot; Definition = definition; }
}
}
