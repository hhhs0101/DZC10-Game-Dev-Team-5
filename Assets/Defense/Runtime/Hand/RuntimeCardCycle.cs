using System;
using System.Collections.Generic;
namespace Defense {
public sealed class RuntimeCardCycle {
    private readonly List<RuntimeCard> hand = new List<RuntimeCard>(3);
    private readonly List<RuntimeCard> upcoming = new List<RuntimeCard>(3);
    public IReadOnlyList<RuntimeCard> Hand { get; }
    public IReadOnlyList<RuntimeCard> UpcomingQueue { get; }
    public IReadOnlyList<RuntimeCard> InitialOrder { get; }
    public event Action Changed;
    public RuntimeCardCycle(PlayerDeck deck, Random random = null) {
        random = random ?? new Random(Guid.NewGuid().GetHashCode());
        var copy = new RuntimeCard[PlayerDeck.SlotCount];
        for (int i=0;i<copy.Length;i++) copy[i] = new RuntimeCard(i,deck.Slots[i]);
        // Fisher-Yates: called only here, never during successful card use.
        for (int i=copy.Length-1;i>0;i--) { int j = random.Next(i+1); var value = copy[i]; copy[i] = copy[j]; copy[j] = value; }
        InitialOrder = Array.AsReadOnly(copy);
        for (int i=0;i<3;i++) { hand.Add(copy[i]); upcoming.Add(copy[i+3]); }
        Hand = hand.AsReadOnly(); UpcomingQueue = upcoming.AsReadOnly();
    }
    public bool Contains(RuntimeCard card) => card != null && hand.Contains(card);
    public bool Use(RuntimeCard card) {
        if (!hand.Remove(card)) return false;
        hand.Add(upcoming[0]); upcoming.RemoveAt(0); upcoming.Add(card);
        Changed?.Invoke(); return true;
    }
}
}
