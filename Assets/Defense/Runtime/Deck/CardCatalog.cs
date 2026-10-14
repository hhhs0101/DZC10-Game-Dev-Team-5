using System;
using System.Collections.Generic;
namespace Defense {
// Persist stable identity, never display names, catalog positions or instance IDs.
public sealed class CardCatalog {
    private readonly Dictionary<string,CardDefinition> cards = new Dictionary<string,CardDefinition>(StringComparer.Ordinal);
    private readonly HashSet<string> ambiguous = new HashSet<string>(StringComparer.Ordinal);
    public CardCatalog(IReadOnlyList<CardDefinition> definitions) {
        foreach (var card in definitions) {
            if (card == null || string.IsNullOrWhiteSpace(card.CardId)) continue;
            if (cards.TryGetValue(card.CardId,out var existing) && existing != card) ambiguous.Add(card.CardId);
            else cards[card.CardId] = card;
        }
    }
    public bool TryResolve(string id,out CardDefinition card) {
        card = null;
        return !string.IsNullOrWhiteSpace(id) && !ambiguous.Contains(id) && cards.TryGetValue(id,out card) && card != null;
    }
}
}
