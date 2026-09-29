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
        if (deck == null) throw new ArgumentNullException(nameof(deck));
        random = random ?? new Random(Guid.NewGuid().GetHashCode());
        var copy = new RuntimeCard[PlayerDeck.SlotCount];
        for (int i=0;i<copy.Length;i++) copy[i] = new RuntimeCard(i,deck.Slots[i]);
        // Fisher-Yates: called only here, never during successful card use.
        for (int i=copy.Length-1;i>0;i--) { int j = random.Next(i+1); var value = copy[i]; copy[i] = copy[j]; copy[j] = value; }
        InitialOrder = Array.AsReadOnly(copy);
        for (int i=0;i<3;i++) { hand.Add(copy[i]); upcoming.Add(copy[i+3]); }
        Hand = hand.AsReadOnly(); UpcomingQueue = upcoming.AsReadOnly();
        ValidateState();
    }
    private void ValidateState() {
        if (hand.Count != 3 || upcoming.Count != 3)
            throw new InvalidOperationException("Runtime cycle must have three Hand and three Upcoming cards.");
        var definitions = new HashSet<TurretDefinition>();
        foreach (var card in hand)
            if (card == null || card.Definition == null || !definitions.Add(card.Definition))
                throw new InvalidOperationException("Duplicate or invalid tower definition in Hand.");
        foreach (var card in upcoming)
            if (card == null || card.Definition == null || !definitions.Add(card.Definition))
                throw new InvalidOperationException("Duplicate or invalid tower definition in runtime cycle.");
    }
    public bool Contains(RuntimeCard card) => card != null && hand.Contains(card);
    public bool Use(RuntimeCard card) {
        ValidateState();
        if (!hand.Remove(card)) return false;
        hand.Add(upcoming[0]); upcoming.RemoveAt(0); upcoming.Add(card);
        ValidateState();
        Changed?.Invoke(); return true;
    }
}
}
