using System;
using System.Collections.Generic;
namespace Defense {
public static class StageCardRules {
    public static IReadOnlyList<CardDefinition> Available(StageDefinition stage, GameCatalog catalog) => Available(stage, catalog, PlayerSession.Progression);

    public static IReadOnlyList<CardDefinition> Available(StageDefinition stage, GameCatalog catalog, StageProgressionState progression) {
        if (stage == null || !stage.HasCardRestrictions) return catalog != null ? catalog.AvailableCards : Array.Empty<CardDefinition>();
        var cards = new List<CardDefinition>(stage.AllowedCards);
        if (catalog != null && catalog.Stages != null && progression != null) {
            var seen = new HashSet<CardDefinition>(cards);
            for (int i = 0; i < catalog.Stages.Count; i++) {
                if (progression.IsComplete(i) && i + 1 < catalog.Stages.Count) {
                    var nextStage = catalog.Stages[i + 1];
                    if (nextStage != null && nextStage.AllowedCards != null) {
                        foreach (var card in nextStage.AllowedCards) {
                            if (card != null && seen.Add(card)) cards.Add(card);
                        }
                    }
                }
            }
        }
        return cards;
    }

    public static IReadOnlyList<CardDefinition> Defaults(StageDefinition stage, GameCatalog catalog) => stage != null && stage.HasCardRestrictions ? stage.DefaultDeck : catalog.InitialDeck;
    public static void Validate(StageDefinition stage, GameCatalog catalog) {
        var available = stage != null && stage.HasCardRestrictions ? stage.AllowedCards : (catalog != null ? catalog.AvailableCards : Array.Empty<CardDefinition>());
        var known = catalog != null ? new HashSet<CardDefinition>(catalog.AvailableCards) : new HashSet<CardDefinition>();
        var unique = new HashSet<CardDefinition>();
        foreach(var card in available) if(card == null || !known.Contains(card) || !unique.Add(card))
            throw new InvalidOperationException("Stage card pool contains a missing, duplicate or unknown card.");
        var defaults = new PlayerDeck(Defaults(stage,catalog));
        if(stage != null && stage.HasCardRestrictions && defaults.Slots.Count != Math.Min(PlayerDeck.SlotCount,unique.Count))
            throw new InvalidOperationException("Stage default deck must use the available cards up to the six-card limit.");
        foreach(var card in defaults.Slots) if(!unique.Contains(card)) throw new InvalidOperationException("Stage default deck contains a locked card.");
    }
    public static string SavePath(StageDefinition stage,string basePath) {
        if(stage == null || !stage.HasCardRestrictions) return basePath;
        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(basePath) ?? "",
            System.IO.Path.GetFileNameWithoutExtension(basePath)+"-"+stage.name+System.IO.Path.GetExtension(basePath));
    }
    public static bool Allows(StageDefinition stage, CardDefinition card) => Allows(stage, card, PlayerSession.Catalog, PlayerSession.Progression);

    public static bool Allows(StageDefinition stage, CardDefinition card, GameCatalog catalog, StageProgressionState progression) {
        if (stage == null || !stage.HasCardRestrictions) return true;
        if (card == null) return false;
        foreach (var allowed in stage.AllowedCards) if (allowed == card) return true;
        if (catalog != null && progression != null) {
            var available = Available(stage, catalog, progression);
            for (int i = 0; i < available.Count; i++) if (available[i] == card) return true;
        }
        return false;
    }
}
}
